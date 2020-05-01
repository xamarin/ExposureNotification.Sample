using Foundation;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ExposureNotifications;
using System.Linq;

namespace Xamarin.ExposureNotifications
{
	public static partial class ExposureNotification
	{
		static ENManager manager;
		static ENExposureDetectionSession session;

		static async Task<ENManager> GetManagerAsync()
		{
			if (manager == null)
			{
				manager = new ENManager();
				await manager.ActivateAsync();
			}

			return manager;
		}

		static async Task<ENExposureDetectionSession> GetSessionAsync()
		{
			if (session == null)
			{
				session = new ENExposureDetectionSession();
				await session.ActivateAsync();
			}

			return session;
		}

		static async Task PlatformStart(IExposureNotificationHandler handler)
		{
			var m = await GetManagerAsync();
			await m.SetExposureNotificationEnabledAsync(true);
		}

		static async Task PlatformStop()
		{
			var m = await GetManagerAsync();
			await m.SetExposureNotificationEnabledAsync(false);
		}

		static async Task<bool> PlatformIsEnabled()
		{
			var m = await GetManagerAsync();
			return m.ExposureNotificationEnabled;
		}

		// Gets the contact info of anyone the user had contact with who was diagnosed
		static async Task<IEnumerable<ExposureInfo>> PlatformGetExposureInformation()
		{
			var s = await GetSessionAsync();
			
			// TODO: Check max
			var info = await s.GetExposureInfoAsync(100);

			return info.Exposures.Select(i =>
				new ExposureInfo(
					TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(2001, 1, 1, 0, 0, 0))
						.AddSeconds(i.Date.SecondsSinceReferenceDate),
					TimeSpan.FromMinutes(i.Duration),
					(int)i.AttenuationValue,
					(byte)i.TotalRiskScore,
					(RiskLevel)i.TransmissionRiskLevel));
		}

		// Call this when the user has confirmed diagnosis
		static async Task PlatformSubmitSelfDiagnosis(UploadKeysToServerDelegate uploadKeysToServerDelegate)
		{
			var m = await GetManagerAsync();
			var selfKeys = await m.GetDiagnosisKeysAsync();

			await ExposureNotification.Handler.SubmitSelfDiagnosisKeysToServer(
				selfKeys.Select(k => new TemporaryExposureKey(
					k.KeyData.ToArray(),
					k.RollingStartNumber,
					TimeSpan.FromMinutes(k.RollingStartNumber),
					(RiskLevel)k.TransmissionRiskLevel)));
		}

		// Tells the local API when new diagnosis keys have been obtained from the server
		static async Task<ExposureDetectionSummary> PlatformAddDiagnosisKeys(IEnumerable<TemporaryExposureKey> diagnosisKeys)
		{
			var s = await GetSessionAsync();

			// Batch up adding our items
			var batchSize = (int)s.MaximumKeyCount;
			var sequence = diagnosisKeys;

			while (sequence.Any())
			{
				var batch = sequence.Take(batchSize);
				sequence = sequence.Skip(batchSize);
				
				await s.AddDiagnosisKeysAsync(diagnosisKeys.Select(k =>
					new ENTemporaryExposureKey
					{
						KeyData = NSData.FromArray(k.KeyData),
						RollingStartNumber = (uint)k.RollingStart
					}).ToArray());
			}

			var summary = await s.FinishedDiagnosisKeysAsync();

			return new ExposureDetectionSummary(
				(int)summary.DaysSinceLastExposure,
				summary.MatchedKeyCount,
				summary.MaximumRiskScore);
		}

		static async Task<IEnumerable<TemporaryExposureKey>> PlatformGetTemporaryExposureKeys()
		{
			var m = await GetManagerAsync();
			var selfKeys = await m.GetDiagnosisKeysAsync();

			return selfKeys.Select(k =>
				new TemporaryExposureKey(k.KeyData.ToArray(), k.RollingStartNumber, new TimeSpan(), (RiskLevel)k.TransmissionRiskLevel));
		}
	}
}
