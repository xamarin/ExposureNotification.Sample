using System;
using System.Linq;
using System.Threading.Tasks;
using ExposureNotification.Backend.Database;
using ExposureNotification.Backend.Signing;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Options;

namespace ExposureNotification.Backend.Functions
{
	public class CreateBatchesFunction
	{
		const string dirNumberMetadataKey = "dir_number";
		const string batchNumberMetadataKey = "batch_number";
		const string batchRegionMetadataKey = "batch_region";

		readonly ExposureNotificationStorage storage;
		readonly IOptions<Settings> settings;
		readonly ISigner signer;

		public CreateBatchesFunction(ExposureNotificationStorage storage, IOptions<Settings> settings, ISigner signer)
		{
			this.storage = storage;
			this.settings = settings;
			this.signer = signer;
		}

		// Every 6 hours
		[FunctionName("CreateBatchesFunction")]
		public Task RunTimed([TimerTrigger("0 0 */6 * * *")] TimerInfo myTimer) => CreateBatchFiles();

		// On demand
		[FunctionName("batch")]
		public Task RunRequest([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req) => CreateBatchFiles();

		async Task CreateBatchFiles()
		{
			var cloudStorageAccount = CloudStorageAccount.Parse(settings.Value.BlobStorageConnectionString);
			var cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();

			foreach (var region in settings.Value.SupportedRegions)
			{
				// We base the container name off a global configurable prefix
				// and also the region name, so we end up having one container per
				// region which can help with azure scaling/region allocation
				var containerName = $"{settings.Value.BlobStorageContainerNamePrefix}{region}";

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
				var signerInfos = await storage.GetAllSignerInfosAsync();

				// Create batch files from all the keys in the database
				await storage.CreateBatchFilesAsync(region, async export =>
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
