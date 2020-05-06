using ExposureNotification.App.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xamarin.Essentials;
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

		public Configuration Configuration
			=> new Configuration();

		public async Task ExposureDetected(ExposureDetectionSummary summary, Func<Task<IEnumerable<ExposureInfo>>> getDetailsFunc)
		{
			LocalStateManager.Instance.ExposureSummary = summary;

			var details = await getDetailsFunc();

			LocalStateManager.Instance.ExposureInformation.AddRange(details);

			LocalStateManager.Save();

			MessagingCenter.Instance.Send(this, "exposure_info_changed");

			// TODO: Save this info and alert the user
			// Pop up a local notification
		}

		public async Task<IEnumerable<TemporaryExposureKey>> FetchExposureKeysFromServer()
		{
			// Get the newest date we have keys from and request since then
			// or if no date stored, only return as much as the past 14 days of keys
			var sinceEpochSeconds = LocalStateManager.Instance.NewestKeysResponseTimestamp.ToUnixTimeSeconds();
			var url = $"{apiUrlBase.TrimEnd('/')}/keys?since={sinceEpochSeconds}";

			var response = await http.GetAsync(url);

			response.EnsureSuccessStatusCode();

			var responseData = await response.Content.ReadAsStringAsync();

			// Response contains the timestamp in seconds since epoch, and the list of keys
			var keys = JsonConvert.DeserializeObject<KeysResponse>(responseData);

			// Save newest timestamp for next request
			LocalStateManager.Instance.NewestKeysResponseTimestamp = DateTimeOffset.FromUnixTimeSeconds(keys.Timestamp);
			LocalStateManager.Save();

			return keys.Keys;
		}

		public async Task UploadSelfExposureKeysToServer(IEnumerable<TemporaryExposureKey> temporaryExposureKeys)
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
			[JsonProperty("timestamp")]
			public long Timestamp { get; set; }

			[JsonProperty("keys")]
			public IEnumerable<TemporaryExposureKey> Keys { get; set; }
		}
	}
}
