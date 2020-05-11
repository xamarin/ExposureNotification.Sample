using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ExposureNotifications;
using Foundation;
using Google.Protobuf;

namespace Xamarin.ExposureNotifications
{
	public static partial class ExposureNotification
	{
		static ENManager manager;

		static async Task<ENManager> GetManagerAsync()
		{
			if (manager == null)
			{
				manager = new ENManager();
				await manager.ActivateAsync();
			}

			return manager;
		}

		static async Task<ENExposureConfiguration> GetConfigurationAsync()
		{
			var c = await Handler.GetConfigurationAsync();

			return new ENExposureConfiguration
			{
				AttenuationLevelValues = c.AttenuationScores,
				DurationLevelValues = c.DurationScores,
				DaysSinceLastExposureLevelValues = c.DaysSinceLastExposureScores,
				TransmissionRiskLevelValues = c.TransmissionRiskScores,
				AttenuationWeight = c.AttenuationWeight,
				DaysSinceLastExposureWeight = c.DaysSinceLastExposureWeight,
				DurationWeight = c.DurationWeight,
				TransmissionRiskWeight = c.TransmissionWeight,
				MinimumRiskScore = (byte)c.MinimumRiskScore
			};
		}

		static async Task PlatformStart()
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

		// Tells the local API when new diagnosis keys have been obtained from the server
		static async Task<(ExposureDetectionSummary, IEnumerable<ExposureInfo>)> PlatformDetectExposuresAsync(IEnumerable<string> keyFiles)
		{
			// Submit to the API
			var c = await GetConfigurationAsync();
			var m = await GetManagerAsync();

			var detectionSummary = await m.DetectExposuresAsync(
				c,
				keyFiles.Select(k => new NSUrl(k, false)).ToArray());

			var summary = new ExposureDetectionSummary(
				(int)detectionSummary.DaysSinceLastExposure,
				detectionSummary.MatchedKeyCount,
				detectionSummary.MaximumRiskScore);

			// Get the info
			IEnumerable<ExposureInfo> info = Array.Empty<ExposureInfo>();
			if (summary?.MatchedKeyCount > 0)
			{
				var exposures = await m.GetExposureInfoAsync(detectionSummary, Handler.UserExplanation);
				info = exposures.Select(i => new ExposureInfo(
					((DateTime)i.Date).ToLocalTime(),
					TimeSpan.FromMinutes(i.Duration),
					i.AttenuationValue,
					i.TotalRiskScore,
					i.TransmissionRiskLevel.FromNative()));
			}

			// Return everything
			return (summary, info);
		}

		static async Task<IEnumerable<TemporaryExposureKey>> PlatformGetTemporaryExposureKeys()
		{
			var m = await GetManagerAsync();
			var selfKeys = await m.GetDiagnosisKeysAsync();

			return selfKeys.Select(k => new TemporaryExposureKey(
				k.KeyData.ToArray(),
				k.RollingStartNumber,
				TimeSpan.FromMinutes(k.RollingPeriod * 10),
				k.TransmissionRiskLevel.FromNative()));
		}
	}

	static partial class Utils
	{
		public static RiskLevel FromNative(this byte riskLevel) =>
			riskLevel switch
			{
				1 => RiskLevel.Lowest,
				2 => RiskLevel.Low,
				3 => RiskLevel.MediumLow,
				4 => RiskLevel.Medium,
				5 => RiskLevel.MediumHigh,
				6 => RiskLevel.High,
				7 => RiskLevel.VeryHigh,
				8 => RiskLevel.Highest,
				_ => RiskLevel.Invalid,
			};

		public static byte ToNative(this RiskLevel riskLevel) =>
			riskLevel switch
			{
				RiskLevel.Lowest => 1,
				RiskLevel.Low => 2,
				RiskLevel.MediumLow => 3,
				RiskLevel.Medium => 4,
				RiskLevel.MediumHigh => 5,
				RiskLevel.High => 6,
				RiskLevel.VeryHigh => 7,
				RiskLevel.Highest => 8,
				_ => 0,
			};
	}
}
