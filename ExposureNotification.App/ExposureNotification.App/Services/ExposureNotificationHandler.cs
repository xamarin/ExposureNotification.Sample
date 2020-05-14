using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using ExposureNotification.App.Services;
using Newtonsoft.Json;
using Plugin.LocalNotification;
using Xamarin.Essentials;
using Xamarin.ExposureNotifications;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace ExposureNotification.App
{
	[Preserve] // Ensure this isn't linked out
	public class ExposureNotificationHandler : IExposureNotificationHandler
	{
		public const string DefaultRegion = "default";

		const string apiUrlBase = "https://exposurenotificationfunctions.azurewebsites.net/api/";
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

			// Add these on main thread in case the UI is visible so it can update
			await Device.InvokeOnMainThreadAsync(() =>
			{
				foreach (var i in exposureInfo)
					LocalStateManager.Instance.ExposureInformation.Add(i);
			});

			LocalStateManager.Save();

			var notification = new NotificationRequest
			{
				NotificationId = 100,
				Title = "Possible COVID-19 Exposure",
				Description = "It is possible you have been exposed to someone who was a confirmed diagnosis of COVID-19.  Tap for more details."
			};

			NotificationCenter.Current.Show(notification);
		}

		// this will be called when they keys need to be collected from the server
		public async Task<IEnumerable<string>> FetchExposureKeyBatchFilesFromServerAsync()
		{
			var downloadedFiles = new List<string>();

			// This is "default" by default
			var region = LocalStateManager.Instance.Region ?? DefaultRegion;

			var stopDownload = false;

			while (!stopDownload)
			{
				// Find next directory to start checking
				var dirNumber = LocalStateManager.Instance.ServerBatchNumber + 1;

				var batchNumber = 1;

				try
				{
					while (true)
					{
						// Build the blob storage url for the given batch file we are on next
						var url = $"{apiUrlBlobStorageBase}/{blobStorageContainerNamePrefix}{region}/{dirNumber}/{batchNumber}.dat";

						var response = await http.GetAsync(url);

						// If we get a 404, there are no newer batch files available to download
						if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
						{
							// Check if batch == 1 which means the first file doesn't exist
							// The dir we tried is empty, we have all the latest, stop downloading
							if (batchNumber == 1)
								stopDownload = true;

							// In any case no more files in this dir
							break;
						}

						response.EnsureSuccessStatusCode();

						// Skip batch files which are older than 14 days
						if (response.Content.Headers.LastModified.HasValue)
						{
							if (response.Content.Headers.LastModified < DateTimeOffset.UtcNow.AddDays(-14))
							{
								batchNumber++;
								continue;
							}
						}

						var tmpFile = Path.Combine(FileSystem.CacheDirectory, Guid.NewGuid().ToString() + ".zip");

						// Read the batch file stream
						using var responseStream = await response.Content.ReadAsStreamAsync();
						using var fileStream = File.Create(tmpFile);
						await responseStream.CopyToAsync(fileStream);

						downloadedFiles.Add(tmpFile);
					}

					LocalStateManager.Instance.ServerBatchNumber = dirNumber;
					LocalStateManager.Save();
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);

					// If this failed for some reason, stop and allow the next job to continue
					stopDownload = true;
				}
			}

			return downloadedFiles;
		}

		// this will be called when the user is submitting a diagnosis and the local keys need to go to the server
		public async Task UploadSelfExposureKeysToServerAsync(IEnumerable<TemporaryExposureKey> temporaryExposureKeys)
		{
			var pendingDiagnosis = LocalStateManager.Instance.PendingDiagnosis;

			if (pendingDiagnosis == null || string.IsNullOrEmpty(pendingDiagnosis.DiagnosisUid))
				throw new InvalidOperationException();

			try
			{
				var url = $"{apiUrlBase.TrimEnd('/')}/selfdiagnosis";

				var json = JsonConvert.SerializeObject(new SelfDiagnosisSubmissionRequest
				{
					DiagnosisUid = pendingDiagnosis.DiagnosisUid,
					TestDate = pendingDiagnosis.DiagnosisDate.ToUnixTimeMilliseconds(),
					Keys = temporaryExposureKeys
				});

				var http = new HttpClient();
				var response = await http.PutAsync(url, new StringContent(json));

				response.EnsureSuccessStatusCode();

				// Update pending status
				pendingDiagnosis.Shared = true;
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
				var json = "{\"diagnosisUid\":\"" + diagnosisUid + "\"}";
				var response = await http.PostAsync(url, new StringContent(json));

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

			[JsonProperty("testDate")]
			public long TestDate { get; set; }

			[JsonProperty("keys")]
			public IEnumerable<TemporaryExposureKey> Keys { get; set; }
		}
	}
}
