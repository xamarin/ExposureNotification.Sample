using System;
using System.Linq;
using System.Threading.Tasks;
using Functions.Database;
using Functions.Signing;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Functions
{
	public class CreateBatchesFunction
	{
		const string dirNumberMetadataKey = "dir_number";
		const string batchNumberMetadataKey = "batch_number";
		const string batchRegionMetadataKey = "batch_region";

		readonly ExposureNotificationStorage storage;
		readonly IOptions<Settings> settings;

		public CreateBatchesFunction(ExposureNotificationStorage storage, IOptions<Settings> settings)
		{
			this.storage = storage;
			this.settings = settings;
		}

		// Every 6 hours
		[FunctionName("CreateBatchesTimed")]
		public Task RunTimed([TimerTrigger("0 0 */6 * * *")] TimerInfo myTimer, ILogger logger)
		{
			logger.LogInformation("Starting timed batching...");

			return CreateBatchFiles(logger);
		}

		// On demand
#if DEBUG
		[FunctionName("CreateBatchesOnDemand")]
#endif
		public Task RunRequest([HttpTrigger(AuthorizationLevel.Function, "get", Route = "manage/start-batch")] HttpRequest req, ILogger logger)
		{
			logger.LogInformation("Starting on-demand batching...");

			return CreateBatchFiles(logger);
		}

		async Task CreateBatchFiles(ILogger logger)
		{
			var supportedRegions = settings.Value.SupportedRegions;
			if (supportedRegions?.Any() != true)
				logger.LogWarning("No supported regions.");

			var cloudStorageAccount = CloudStorageAccount.Parse(settings.Value.BlobStorageConnectionString);
			var cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();

			foreach (var region in supportedRegions)
			{
				if (!await storage.HasKeysAsync(region))
				{
					logger.LogInformation("No keys found for region '{0}'.", region);
					continue;
				}

				// We base the container name off a global configurable prefix
				// and also the region name, so we end up having one container per
				// region which can help with azure scaling/region allocation
				var containerName = $"{settings.Value.BlobStorageContainerNamePrefix}{region.ToLowerInvariant()}";

				logger.LogInformation("Batch may be saved to container '{0}'.", containerName);

				// Get our container
				var cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);

				// Make sure the container exists
				await cloudBlobContainer.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Container, new BlobRequestOptions(), new OperationContext());

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

				logger.LogInformation("Batch may be saved to path '{0}/{1}'.", containerName, nextDirNumber);

				// Load all signer infos
				var signerInfos = await storage.GetAllSignerInfosAsync();

				var batchesCount = 0;
				var batchFileCount = 0;

				do
				{
					// Create batch files from all the keys in the database
					batchFileCount = await storage.CreateBatchFilesAsync(region, settings.Value.MaxFilesPerBatch, async export =>
					{
						// Don't process a batch without keys
						if (export == null || (export.Keys != null && export.Keys.Count() <= 0))
						{
							logger.LogWarning("For some reason, a batch was started when there were no keys to put in that batch...");
							return;
						}

						// Filename is inferable as batch number
						var batchFileName = $"{nextDirNumber}/{export.BatchNum}.dat";

						var blockBlob = cloudBlobContainer.GetBlockBlobReference(batchFileName);

						// Write the proto buf to a memory stream
						using var signedFileStream = await ExposureBatchFileUtil.CreateSignedFileAsync(export, signerInfos);

						// Set the batch number and region as metadata
						blockBlob.Metadata[dirNumberMetadataKey] = nextDirNumber.ToString();
						blockBlob.Metadata[batchNumberMetadataKey] = export.BatchNum.ToString();
						blockBlob.Metadata[batchRegionMetadataKey] = region;

						await blockBlob.UploadFromStreamAsync(signedFileStream);
						await blockBlob.SetMetadataAsync();

						logger.LogInformation($"Saved batch file '{export.BatchNum}/{export.BatchSize}' to '{containerName}/{nextDirNumber}/{export.BatchNum}.dat'.");
					});

					logger.LogInformation($"Saved {batchFileCount} batch files to '{containerName}/{nextDirNumber}/'.");

					if (batchFileCount > 0)
					{
						// Increment our dir number for the next batch to be created
						nextDirNumber++;
						batchesCount++;
					}

				} while (batchFileCount > 0);

				logger.LogInformation($"Saved {batchesCount} batches for {region}.");
			}
		}
	}
}
