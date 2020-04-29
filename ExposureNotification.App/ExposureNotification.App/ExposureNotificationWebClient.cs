using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace ExposureNotification.App
{
	public class ExposureNotificationWebClient
	{
		static readonly HttpClient http = new HttpClient();

		public ExposureNotificationWebClient(string webServiceUrlBase, ITemporaryExposureKeyEncoding encoder)
		{
			WebServiceUrlBase = webServiceUrlBase;
			Encoder = encoder;
		}

		public string WebServiceUrlBase { get; }

		public ITemporaryExposureKeyEncoding Encoder { get; }

		public async Task GetKeysAsync()
		{
			const string prefsSinceKey = "keys_since";

			// Get the newest date we have keys from and request since then
			// or if no date stored, only return as much as the past 14 days of keys
			var since = Preferences.Get(prefsSinceKey, DateTime.UtcNow.AddDays(-14));

			var url = GetUrl($"keys?since={since:o}");

			var response = await http.GetAsync(url);

			response.EnsureSuccessStatusCode();

			var json = await response.Content.ReadAsStringAsync();

			var keys = JsonSerializer.Deserialize<IEnumerable<TemporaryExposureKey>>(json);

			// Find newest key timestamp 
			var newestTimestamp = keys?.OrderByDescending(k => k.Timestamp).FirstOrDefault()?.Timestamp;

			// Save newest timestamp for next request
			if (newestTimestamp.HasValue)
				Preferences.Set(prefsSinceKey, newestTimestamp.Value.ToUniversalTime());
		}

		const string prefsDiagnosisUidKey = "diagnosis_uid";

		public Task SubmitPositiveDiagnosisAsync(IEnumerable<Xamarin.ExposureNotifications.TemporaryExposureKey> keys)
			=> SubmitPositiveDiagnosisAsync(Preferences.Get(prefsDiagnosisUidKey, (string)default), keys);

		public async Task SubmitPositiveDiagnosisAsync(string diagnosisUid, IEnumerable<Xamarin.ExposureNotifications.TemporaryExposureKey> keys)
		{
			if (string.IsNullOrEmpty(diagnosisUid))
				throw new InvalidOperationException();

			var url = GetUrl($"diagnosis");

			var encodedKeys = keys.Select(k => new TemporaryExposureKey
			{
				KeyData = Encoder.Encode(k.KeyData),
				Timestamp = k.Timestamp
			});

			var json = JsonSerializer.Serialize(new DiagnosisSubmission
			{
				DiagnosisUid = diagnosisUid,
				TemporaryExposureKeys = encodedKeys.ToList()
			});

			var response = await http.PostAsync(url, new StringContent(json));

			response.EnsureSuccessStatusCode();

			Preferences.Set(prefsDiagnosisUidKey, diagnosisUid);
		}

		string GetUrl(string path)
			=> WebServiceUrlBase.TrimEnd('/') + path;
	}
}
