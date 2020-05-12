using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Xamarin.ExposureNotifications;

namespace ExposureNotification.Backend.Functions
{
	public static class CreateBatchesFunction
	{
		const string batchNumberMetadataKey = "batch_number";
		const string batchRegionMetadataKey = "batch_region";

		// Every 6 hours
		[FunctionName("CreateBatchesFunction")]
		public static async Task Run([TimerTrigger("* * */6 * * *")]TimerInfo myTimer, ILogger log)
		{
			var cloudStorageAccount = CloudStorageAccount.Parse(Startup.BlobStorageConnectionString);
			var cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();

			foreach (var region in Startup.ExposureKeyRegions)
			{
				// We base the container name off a global configurable prefix
				// and also the region name, so we end up having one container per
				// region which can help with azure scaling/region allocation
				var containerName = $"{Startup.BlobStorageContainerNamePrefix}{region}";

				var cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);

				// Make sure the container exists
				await cloudBlobContainer.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Blob, new BlobRequestOptions(), new OperationContext());

				// Look at existing batch files
				var existingFiles = await cloudBlobContainer.ListBlobsSegmentedAsync(string.Empty, true, BlobListingDetails.Metadata, null, null, null, null);

				// Start at batch number 1, and increment as we find files for newer batches
				var nextBatchNumber = 1;

				foreach (var blob in existingFiles.Results)
				{
					if (blob is CloudBlockBlob blockBlob)
					{
						// Batch number is stored as metadata (but also is the filename)
						if (int.TryParse(blockBlob.Metadata[batchNumberMetadataKey], out var bn))
						{
							var potentialNext = bn + 1;
							if (potentialNext > nextBatchNumber)
								nextBatchNumber = potentialNext;
						}
					}
				}

				// Keep track of number of keys in each batch request
				var keysInBatch = 0;

				do
				{
					keysInBatch = await Startup.Database.GetNextBatchAsync(nextBatchNumber, region, async batchFile =>
					{
						// Don't process a batch without keys
						if (batchFile == null || (batchFile.Key != null && batchFile.Key.Count() <= 0))
							return;

						// Filename is inferable as batch number
						var batchFileName = $"{nextBatchNumber}.dat";

						var blockBlob = cloudBlobContainer.GetBlockBlobReference(batchFileName);

						// Write the proto buf to a memory stream
						using var memoryStream = new MemoryStream();
						batchFile.WriteTo(memoryStream);
						memoryStream.Seek(0, SeekOrigin.Begin);

						// Set the batch number and region as metadata
						blockBlob.Metadata[batchNumberMetadataKey] = nextBatchNumber.ToString();
						blockBlob.Metadata[batchRegionMetadataKey] = region;

						await blockBlob.UploadFromStreamAsync(memoryStream);
						await blockBlob.SetMetadataAsync();
					});

					nextBatchNumber++;
				} // While we have a full batch, there might be more so request more
				while (keysInBatch >= TemporaryExposureKeyBatches.MaxKeysPerFile);
			}
		}
	}
}
