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
		const string apiUrlBase = "http://localhost:7071/api/";

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
		public async Task FetchExposureKeysFromServerAsync(Func<IEnumerable<TemporaryExposureKey>, Task> processKeyBatchDelegate)
		{
			var latestKeysResponseIndex = LocalStateManager.Instance.LatestKeysResponseIndex;

			var take = 1024;
			var skip = 0;

			bool checkForMore;
			do
			{
				// Get the newest date we have keys from and request since then
				// or if no date stored, only return as much as the past 14 days of keys
				var url = $"{apiUrlBase.TrimEnd('/')}/keys?since={latestKeysResponseIndex}&skip={skip}&take={take}";

				var response = await http.GetAsync(url);

				response.EnsureSuccessStatusCode();

				var responseData = await response.Content.ReadAsStringAsync();

				if (string.IsNullOrEmpty(responseData))
					break;

				// Response contains the timestamp in seconds since epoch, and the list of keys
				var keys = JsonConvert.DeserializeObject<KeysResponse>(responseData);

				var numKeys = keys?.Keys?.Count() ?? 0;

				// See if keys were returned on this call
				if (numKeys > 0)
				{
					// Call the callback with the batch of keys to add
					await processKeyBatchDelegate(keys.Keys);

					var newLatestKeysResponseIndex = keys.Latest;

					if (newLatestKeysResponseIndex > LocalStateManager.Instance.LatestKeysResponseIndex)
					{
						LocalStateManager.Instance.LatestKeysResponseIndex = newLatestKeysResponseIndex;
						LocalStateManager.Save();
					}

					// Increment our skip starting point for the next batch
					skip += take;
				}

				// If we got back more or the same amount of our requested take, there may be
				// more left on the server to request again
				checkForMore = numKeys >= take;

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
				var response = await http.PostAsync(url, new StringContent(diagnosisUid));

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

		class KeysResponse
		{
			[JsonProperty("latest")]
			public ulong Latest { get; set; }

			[JsonProperty("keys")]
			public IEnumerable<TemporaryExposureKey> Keys { get; set; }
		}
	}
}
