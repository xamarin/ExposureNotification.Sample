using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExposureNotifications;
using Foundation;

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
		static async IAsyncEnumerable<ExposureInfo> PlatformGetExposureInformation()
		{
			var s = await GetSessionAsync();

			ENExposureDetectionSessionGetExposureInfoResult result;
			do
			{
				// get a batch of 100
				result = await s.GetExposureInfoAsync(100);

				// return them all
				foreach (var i in result.Exposures)
					yield return new ExposureInfo(
						((DateTime)i.Date).ToLocalTime(),
						TimeSpan.FromMinutes(i.Duration),
						i.AttenuationValue,
						i.TotalRiskScore,
						i.TransmissionRiskLevel.FromNative());
			} while (!result.Done);
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
					k.TransmissionRiskLevel.FromNative())));
		}

		// Tells the local API when new diagnosis keys have been obtained from the server
		static async Task PlatformAddDiagnosisKeys(IEnumerable<TemporaryExposureKey> diagnosisKeys)
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
						TransmissionRiskLevel = k.TransmissionRiskLevel.ToNative(),
					}).ToArray());
			}
		}

		static async Task<ExposureDetectionSummary> PlatformFinishAddDiagnosisKeys()
		{
			var s = await GetSessionAsync();

			var summary = await s.FinishedDiagnosisKeysAsync();

			return new ExposureDetectionSummary(
				(int)summary.DaysSinceLastExposure,
				summary.MatchedKeyCount,
				summary.MaximumRiskScore);
		}

		static async IAsyncEnumerable<TemporaryExposureKey> PlatformGetTemporaryExposureKeys()
		{
			var m = await GetManagerAsync();
			var selfKeys = await m.GetDiagnosisKeysAsync();

			foreach (var k in selfKeys)
				yield return new TemporaryExposureKey(k.KeyData.ToArray(), k.RollingStartNumber, new TimeSpan(), k.TransmissionRiskLevel.FromNative());
		}
	}

	static partial class Utils
	{
		public static RiskLevel FromNative(this ENRiskLevel riskLevel) =>
			riskLevel switch
			{
				ENRiskLevel.Lowest => RiskLevel.Lowest,
				ENRiskLevel.Low => RiskLevel.Low,
				ENRiskLevel.LowMedium => RiskLevel.MediumLow,
				ENRiskLevel.Medium => RiskLevel.Medium,
				ENRiskLevel.MediumHigh => RiskLevel.MediumHigh,
				ENRiskLevel.High => RiskLevel.High,
				ENRiskLevel.VeryHigh => RiskLevel.VeryHigh,
				ENRiskLevel.Highest => RiskLevel.Highest,
				_ => RiskLevel.Invalid,
			};

		public static ENRiskLevel ToNative(this RiskLevel riskLevel) =>
			riskLevel switch
			{
				RiskLevel.Lowest => ENRiskLevel.Lowest,
				RiskLevel.Low => ENRiskLevel.Low,
				RiskLevel.MediumLow => ENRiskLevel.LowMedium,
				RiskLevel.Medium => ENRiskLevel.Medium,
				RiskLevel.MediumHigh => ENRiskLevel.MediumHigh,
				RiskLevel.High => ENRiskLevel.High,
				RiskLevel.VeryHigh => ENRiskLevel.VeryHigh,
				RiskLevel.Highest => ENRiskLevel.Highest,
				_ => ENRiskLevel.Invalid,
			};
	}
}
