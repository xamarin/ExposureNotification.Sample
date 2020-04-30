using ExposureNotification.Core;
using Newtonsoft.Json;
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

			var url = GetUrl($"keys?since={since:o}");

			var response = await http.GetAsync(url);

			response.EnsureSuccessStatusCode();

			var json = await response.Content.ReadAsStringAsync();

			var keys = JsonConvert.DeserializeObject<KeysResponse>(json);

			// Save newest timestamp for next request
			Preferences.Set(prefsSinceKey, keys.Timestamp.ToUniversalTime());

			return keys.Keys;
		}

		internal const string PrefsDiagnosisUidKey = "diagnosis_uid";

		public async Task SubmitSelfDiagnosisKeysToServer(IEnumerable<TemporaryExposureKey> temporaryExposureKeys)
		{
			var diagnosisUid = Preferences.Get(PrefsDiagnosisUidKey, (string)null);
			if (string.IsNullOrEmpty(diagnosisUid))
				throw new InvalidOperationException();

			X509Certificate2 cert = null;

			using (var s = Assembly.GetCallingAssembly().GetManifestResourceStream(Config.CertificateResourceFilename))
			using (var m = new MemoryStream())
			{
				await s.CopyToAsync(m);
				m.Position = 0;

				cert = new X509Certificate2(m.ToArray());
			}

			var encoder = new DefaultTemporaryExposureKeyEncoder(cert);

			var url = GetUrl($"diagnosis");

			var encodedKeys = temporaryExposureKeys.Select(k => new TemporaryExposureKey
			{
				KeyData = encoder.Encode(k.KeyData),
				RollingStart = k.RollingStart,
				RollingDuration = k.RollingDuration,
				TransmissionRiskLevel = k.TransmissionRiskLevel
			});

			var json = JsonConvert.SerializeObject(new DiagnosisSubmission
			{
				DiagnosisUid = diagnosisUid,
				TemporaryExposureKeys = encodedKeys.ToList()
			});

			var response = await http.PostAsync(url, new StringContent(json));

			response.EnsureSuccessStatusCode();

			Preferences.Set(PrefsDiagnosisUidKey, diagnosisUid);
		}

		static string GetUrl(string path)
			=> Config.ApiUrlBase.TrimEnd('/') + path;
	}
}
