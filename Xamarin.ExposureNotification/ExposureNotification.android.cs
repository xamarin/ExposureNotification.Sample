using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Gms.Nearby.ExposureNotification;

using Nearby = Android.Gms.Nearby.NearbyClass;
using AndroidRiskLevel = Android.Gms.Nearby.ExposureNotification.RiskLevel;
using TemporaryExposureKeyBuilder = Android.Gms.Nearby.ExposureNotification.TemporaryExposureKey.TemporaryExposureKeyBuilder;
using Google.Protobuf;

[assembly: UsesPermission(Android.Manifest.Permission.Bluetooth)]

namespace Xamarin.ExposureNotifications
{
	public static partial class ExposureNotification
	{
		const int diagnosisFileMaxKeys = 18000;

		static IExposureNotificationClient instance;

		static IExposureNotificationClient Instance
			=> instance ??= Nearby.GetExposureNotificationClient(Application.Context);

		static async Task<ExposureConfiguration> GetConfigurationAsync()
		{
			var c = await Handler.GetConfigurationAsync();

			return new ExposureConfiguration.ExposureConfigurationBuilder()
				.SetAttenuationScores(c.AttenuationScores)
				.SetDurationScores(c.DurationScores)
				.SetDaysSinceLastExposureScores(c.DaysSinceLastExposureScores)
				.SetTransmissionRiskScores(c.TransmissionRiskScores)
				.SetAttenuationWeight(c.AttenuationWeight)
				.SetDaysSinceLastExposureWeight(c.DaysSinceLastExposureWeight)
				.SetDurationWeight(c.DurationWeight)
				.SetTransmissionRiskWeight(c.TransmissionWeight)
				.SetMinimumRiskScore(c.MinimumRiskScore)
				.Build();
		}

		static Task PlatformStart()
			=> Instance.StartAsync();

		static Task PlatformStop()
			=> Instance.StopAsync();

		static async Task<bool> PlatformIsEnabled()
			=> await Instance.IsEnabledAsync();

		// Tells the local API when new diagnosis keys have been obtained from the server
		static async Task PlatformDetectExposuresAsync(IEnumerable<TemporaryExposureKey> diagnosisKeys)
		{
			var config = await GetConfigurationAsync();

			// Get a temporary working directory
			var tempFiles = new List<Java.IO.File>();

			try
			{
				// Batch up the keys and save into temporary files
				var sequence = diagnosisKeys;
				while (sequence.Any())
				{
					var batch = sequence.Take(diagnosisFileMaxKeys);
					sequence = sequence.Skip(diagnosisFileMaxKeys);

					var file = new Proto.File();
					file.Key.AddRange(batch.Select(k => new Proto.Key
					{
						KeyData = ByteString.CopyFrom(k.KeyData),
						RollingStartNumber = (uint)k.RollingStartLong,
						RollingPeriod = (uint)(k.RollingDuration.TotalMinutes / 10.0),
						TransmissionRiskLevel = k.TransmissionRiskLevel.ToNative(),
					}));

					var batchFilePath = Java.IO.File.CreateTempFile("diagnosiskeys-", "dat");
					batchFilePath.DeleteOnExit();

					using var stream = System.IO.File.OpenWrite(batchFilePath.AbsolutePath);
					using var coded = new CodedOutputStream(stream);
					file.WriteTo(coded);

					tempFiles.Add(batchFilePath);
				}

				await Instance.ProvideDiagnosisKeysAsync(tempFiles, config, Guid.NewGuid().ToString());
			}
			finally
			{
				// Clean up
				foreach (var f in tempFiles)
				{
					try { f.Delete(); }
					catch { }
				}
			}
		}

		static async Task<IEnumerable<TemporaryExposureKey>> PlatformGetTemporaryExposureKeys()
		{
			var exposureKeyHistory = await Instance.GetTemporaryExposureKeyHistoryAsync();

			return exposureKeyHistory.Select(k =>
				new TemporaryExposureKey(
					k.GetKeyData(),
					k.RollingStartIntervalNumber,
					TimeSpan.Zero, // TODO: TimeSpan.FromMinutes(k.RollingDuration * 10),
					k.TransmissionRiskLevel.FromNative()));
		}

		internal static async Task<IEnumerable<ExposureInfo>> PlatformGetExposureInformationAsync(string token)
		{
			var exposures = await Instance.GetExposureInformationAsync(token);
			var info = exposures.Select(d => new ExposureInfo(
				DateTimeOffset.UnixEpoch.AddMilliseconds(d.DateMillisSinceEpoch).UtcDateTime,
				TimeSpan.FromMinutes(d.DurationMinutes),
				d.AttenuationValue,
				d.TotalRiskScore,
				d.TransmissionRiskLevel.FromNative()));
			return info;
		}

		internal static async Task<ExposureDetectionSummary> PlatformGetExposureSummaryAsync(string token)
		{
			var summary = await Instance.GetExposureSummaryAsync(token);

			// TODO: Reevaluate byte usage here
			return new ExposureDetectionSummary(
				summary.DaysSinceLastExposure,
				(ulong)summary.MatchedKeyCount,
				(byte)summary.MaximumRiskScore);
		}
	}

	static partial class Utils
	{
		public static RiskLevel FromNative(this int riskLevel) =>
			riskLevel switch
			{
				AndroidRiskLevel.RiskLevelLowest => RiskLevel.Lowest,
				AndroidRiskLevel.RiskLevelLow => RiskLevel.Low,
				AndroidRiskLevel.RiskLevelLowMedium => RiskLevel.MediumLow,
				AndroidRiskLevel.RiskLevelMedium => RiskLevel.Medium,
				AndroidRiskLevel.RiskLevelMediumHigh => RiskLevel.MediumHigh,
				AndroidRiskLevel.RiskLevelHigh => RiskLevel.High,
				AndroidRiskLevel.RiskLevelVeryHigh => RiskLevel.VeryHigh,
				AndroidRiskLevel.RiskLevelHighest => RiskLevel.Highest,
				_ => AndroidRiskLevel.RiskLevelInvalid,
			};

		public static int ToNative(this RiskLevel riskLevel) =>
			riskLevel switch
			{
				RiskLevel.Lowest => AndroidRiskLevel.RiskLevelLowest,
				RiskLevel.Low => AndroidRiskLevel.RiskLevelLow,
				RiskLevel.MediumLow => AndroidRiskLevel.RiskLevelLowMedium,
				RiskLevel.Medium => AndroidRiskLevel.RiskLevelMedium,
				RiskLevel.MediumHigh => AndroidRiskLevel.RiskLevelMediumHigh,
				RiskLevel.High => AndroidRiskLevel.RiskLevelHigh,
				RiskLevel.VeryHigh => AndroidRiskLevel.RiskLevelVeryHigh,
				RiskLevel.Highest => AndroidRiskLevel.RiskLevelHighest,
				_ => AndroidRiskLevel.RiskLevelInvalid,
			};
	}
}
