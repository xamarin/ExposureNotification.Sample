using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Resources;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.ExposureNotifications;

namespace ExposureNotification.App
{
	public class ExposureNotificationHandler : IExposureNotificationHandler
	{
		const string ApiUrlBase = "http://localhost:7071/api/";

		static readonly HttpClient http = new HttpClient();

		public Configuration Configuration
			=> new Configuration();

		public async Task ExposureDetected(ExposureDetectionSummary summary, Func<Task<IEnumerable<ExposureInfo>>> getDetailsFunc)
		{
			// TODO: Save this info and alert the user
			// Pop up a local notification
		}

		public async Task<IEnumerable<TemporaryExposureKey>> FetchExposureKeysFromServer()
		{
			const string prefsSinceKey = "keys_since";

			// Get the newest date we have keys from and request since then
			// or if no date stored, only return as much as the past 14 days of keys
			var since = Preferences.Get(prefsSinceKey, DateTime.UtcNow.AddDays(-14));
			var sinceEpochSeconds = new DateTimeOffset(since).ToUnixTimeSeconds();

			var url = $"{ApiUrlBase.TrimEnd('/')}/keys?since={sinceEpochSeconds}";

			var response = await http.GetAsync(url);

			response.EnsureSuccessStatusCode();

			var responseData = await response.Content.ReadAsStringAsync();

			var keys = JsonConvert.DeserializeObject<(DateTime timestamp, List<TemporaryExposureKey> keys)>(responseData);

			// Save newest timestamp for next request
			Preferences.Set(prefsSinceKey, keys.timestamp.ToUniversalTime());

			return keys.keys;
		}

		const string PrefsDiagnosisSubmissionDate = "prefs_diagnosis_submit_date";
		const string PrefsDiagnosisSubmissionUid = "prefs_diagnosis_submit_uid";

		public static bool HasSubmittedDiagnosis
			=> Preferences.Get(PrefsDiagnosisSubmissionDate, DateTime.MinValue)
				>= DateTime.UtcNow.AddDays(-14);

		public static string DiagnosisUid
		{
			get => Preferences.Get(PrefsDiagnosisSubmissionUid, (string)null);
			set => Preferences.Set(PrefsDiagnosisSubmissionUid, value);
		}

		public async Task UploadSelfExposureKeysToServer(IEnumerable<TemporaryExposureKey> temporaryExposureKeys)
		{
			var diagnosisUid = DiagnosisUid;

			if (string.IsNullOrEmpty(diagnosisUid))
				throw new InvalidOperationException();

			try
			{
				var url = $"{ApiUrlBase.TrimEnd('/')}/selfdiagnosis";

				var json = JsonConvert.SerializeObject((diagnosisUid, temporaryExposureKeys));

				var http = new HttpClient();
				var response = await http.PostAsync(url, new StringContent(json));

				response.EnsureSuccessStatusCode();

				// Store the date we were diagnosed
				Preferences.Set(PrefsDiagnosisSubmissionDate, DateTime.UtcNow);
			}
			catch
			{
				// Reset diagnosis status since we don't have one that was successfully submitted
				// and then re-throw
				Preferences.Set(PrefsDiagnosisSubmissionDate, DateTime.UtcNow.AddDays(-100));
				throw;
			}
		}
	}
}
