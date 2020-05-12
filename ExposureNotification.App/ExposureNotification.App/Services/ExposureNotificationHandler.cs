using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ExposureNotification.App.Services;
using Newtonsoft.Json;
using Xamarin.ExposureNotifications;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace ExposureNotification.App
{
	[Preserve]
	public class ExposureNotificationHandler : IExposureNotificationHandler
	{
		public const string DefaultRegion = "default";

		const string apiUrlBase = "http://localhost:7071/api/";
		const string apiUrlBlobStorageBase = "https://exposurenotifications.blob.core.windows.net/";
		const string blobStorageContainerNamePrefix = "";


		static readonly HttpClient http = new HttpClient();

		// this string should be localized
		public string UserExplanation
			=> "We need to make use of the keys to keep you healthy.";

		// this configuration should be obtained from a server and it should be cached locally/in memory as it may be called multiple times
		public Task<Configuration> GetConfigurationAsync()
			=> Task.FromResult(new Configuration());

		// this will be called when a potential exposure has been detected
		public async Task ExposureDetectedAsync(ExposureDetectionSummary summary, IEnumerable<ExposureInfo> exposureInfo)
		{
			LocalStateManager.Instance.ExposureSummary = summary;

			LocalStateManager.Instance.ExposureInformation.AddRange(exposureInfo);

			LocalStateManager.Save();

			MessagingCenter.Instance.Send(this, "exposure_info_changed");

			// TODO: Save this info and alert the user
			// Pop up a local notification
		}

		// this will be called when they keys need to be collected from the server
		public async Task FetchExposureKeysFromServerAsync(ITemporaryExposureKeyBatches batches)
		{
			// This is "default" by default
			var region = LocalStateManager.Instance.Region ?? DefaultRegion;

			var checkForMore = true;
			do
			{
				try
				{
					// Find next batch number
					var batchNumber = LocalStateManager.Instance.ServerBatchNumber + 1;

					// Build the blob storage url for the given batch file we are on next
					var url = $"{apiUrlBlobStorageBase}/{blobStorageContainerNamePrefix}{region}/{batchNumber}.dat";

					var response = await http.GetAsync(url);

					// If we get a 404, there are no newer batch files available to download
					if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
					{
						checkForMore = false;
						break;
					}

					response.EnsureSuccessStatusCode();

					// Skip batch files which are older than 14 days
					if (response.Content.Headers.LastModified.HasValue)
					{
						if (response.Content.Headers.LastModified < DateTimeOffset.UtcNow.AddDays(-14))
						{
							LocalStateManager.Instance.ServerBatchNumber = batchNumber;
							LocalStateManager.Save();
							checkForMore = true;
							continue;
						}
					}

					// Read the batch file stream
					using var responseStream = await response.Content.ReadAsStreamAsync();

					// Parse into a Proto.File
					var batchFile = TemporaryExposureKeyBatch.Parser.ParseFrom(responseStream);

					// Submit to the batch processor
					await batches.AddBatchAsync(batchFile);

					// Update the number we are on
					LocalStateManager.Instance.ServerBatchNumber = batchNumber;
					LocalStateManager.Save();
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
					checkForMore = false;
				}
			} while (checkForMore);
		}

		// this will be called when the user is submitting a diagnosis and the local keys need to go to the server
		public async Task UploadSelfExposureKeysToServerAsync(IEnumerable<TemporaryExposureKey> temporaryExposureKeys)
		{
			var diagnosisUid = LocalStateManager.Instance.LatestDiagnosis.DiagnosisUid;

			if (string.IsNullOrEmpty(diagnosisUid))
				throw new InvalidOperationException();

			try
			{
				var url = $"{apiUrlBase.TrimEnd('/')}/selfdiagnosis";

				var json = JsonConvert.SerializeObject(new SelfDiagnosisSubmissionRequest
				{
					DiagnosisUid = diagnosisUid,
					Keys = temporaryExposureKeys
				});

				var http = new HttpClient();
				var response = await http.PutAsync(url, new StringContent(json));

				response.EnsureSuccessStatusCode();

				LocalStateManager.Instance.LatestDiagnosis.Shared = true;
				LocalStateManager.Save();
			}
			catch
			{
				throw;
			}
		}

		internal static async Task<bool> VerifyDiagnosisUid(string diagnosisUid)
		{
			var url = $"{apiUrlBase.TrimEnd('/')}/selfdiagnosis";

			var http = new HttpClient();

			try
			{
				var response = await http.PostAsync(url, new StringContent("{\"diagnosisUid\":\"" + diagnosisUid + "\"}"));

				response.EnsureSuccessStatusCode();

				return true;
			}
			catch
			{
				return false;
			}
		}

		class SelfDiagnosisSubmissionRequest
		{
			[JsonProperty("diagnosisUid")]
			public string DiagnosisUid { get; set; }

			[JsonProperty("keys")]
			public IEnumerable<TemporaryExposureKey> Keys { get; set; }
		}
	}
}
