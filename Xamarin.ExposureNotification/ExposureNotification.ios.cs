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
				var c = Handler.Configuration;

				session = new ENExposureDetectionSession();
				session.Configuration = new ENExposureConfiguration
				{
					AttenuationScores = c.AttenuationScores,
					DurationScores = c.DurationScores,
					DaysSinceLastExposureScores = c.DaysSinceLastExposureScores,
					TransmissionRiskScores = c.TransmissionRiskScores,
					AttenuationWeight = c.AttenuationWeight,
					DaysSinceLastExposureWeight = c.DaysSinceLastExposureWeight,
					DurationWeight = c.DurationWeight,
					TransmissionRiskWeight = c.TransmissionWeight,
					MinimumRiskScore = (byte)c.MinimumRiskScore
				};
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
					((DateTime)i.Date).ToLocalTime(),
					TimeSpan.FromMinutes(i.Duration),
					i.AttenuationValue,
					i.TotalRiskScore,
					(RiskLevel)i.TransmissionRiskLevel));
		}

		// Call this when the user has confirmed diagnosis
		static async Task PlatformSubmitSelfDiagnosis()
		{
			var m = await GetManagerAsync();
			var selfKeys = await m.GetDiagnosisKeysAsync();

			await Handler.UploadSelfExposureKeysToServer(
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
						RollingStartNumber = (uint)k.RollingStartLong,
						TransmissionRiskLevel = (ENRiskLevel)k.TransmissionRiskLevel
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
