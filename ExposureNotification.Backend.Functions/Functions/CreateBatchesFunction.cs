using System;
using System.Linq;
using System.Threading.Tasks;
using ExposureNotification.Backend.Signing;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace ExposureNotification.Backend.Functions
{
	public class CreateBatchesFunction
	{
		const string dirNumberMetadataKey = "dir_number";
		const string batchNumberMetadataKey = "batch_number";
		const string batchRegionMetadataKey = "batch_region";

		readonly ISigner signer;

		public CreateBatchesFunction(ISigner signer)
		{
			this.signer = signer;
		}

		// Every 6 hours "0 0 */6 * * *"
		[FunctionName("CreateBatchesFunction")]
		public async Task Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req, ILogger log)
		{
			var cloudStorageAccount = CloudStorageAccount.Parse(Startup.BlobStorageConnectionString);
			var cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();

			foreach (var region in Startup.SupportedRegions)
			{
				// We base the container name off a global configurable prefix
				// and also the region name, so we end up having one container per
				// region which can help with azure scaling/region allocation
				var containerName = $"{Startup.BlobStorageContainerNamePrefix}{region}";

				// Get our container
				var cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);

				// Make sure the container exists
				await cloudBlobContainer.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Blob, new BlobRequestOptions(), new OperationContext());

				// Find all the root level directories
				var rootBlobs = cloudBlobContainer.ListBlobs()
					.Where(b => string.IsNullOrEmpty(b.Parent?.Prefix))
					.OfType<CloudBlobDirectory>()
					.ToList();

				var highestDirNumber = 0;

				foreach (var rb in rootBlobs)
				{
					var trimmedPrefix = rb.Prefix.Trim('/');

					if (trimmedPrefix.Contains('/'))
						continue;

					if (int.TryParse(trimmedPrefix, out var num))
						highestDirNumber = Math.Max(highestDirNumber, num);
				}

				// Actual next is plus one
				var nextDirNumber = highestDirNumber + 1;

				// Load all signer infos
				var signerInfos = await Startup.Database.GetAllSignerInfosAsync();

				// Create batch files from all the keys in the database
				await Startup.Database.CreateBatchFilesAsync(region, async export =>
				{
					// Don't process a batch without keys
					if (export == null || (export.Keys != null && export.Keys.Count() <= 0))
						return;

					// Filename is inferable as batch number
					var batchFileName = $"{nextDirNumber}/{export.BatchNum}.dat";

					var blockBlob = cloudBlobContainer.GetBlockBlobReference(batchFileName);

					// Write the proto buf to a memory stream
					using var signedFileStream = await ExposureBatchFileUtil.CreateSignedFileAsync(export, signerInfos, signer);

					// Set the batch number and region as metadata
					blockBlob.Metadata[dirNumberMetadataKey] = nextDirNumber.ToString();
					blockBlob.Metadata[batchNumberMetadataKey] = export.BatchNum.ToString();
					blockBlob.Metadata[batchRegionMetadataKey] = region;

					await blockBlob.UploadFromStreamAsync(signedFileStream);
					await blockBlob.SetMetadataAsync();
				});
			}
		}
	}
}
