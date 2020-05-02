using Android.AccessibilityServices;
using Android.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AndroidPendingIntent = global::Android.App.PendingIntent;
using Android.Gms.ExposureNotifications;
using Android.App;

[assembly: UsesPermission(Android.Manifest.Permission.Bluetooth)]

namespace Xamarin.ExposureNotifications
{
	public static partial class ExposureNotification
	{
		static ExposureNotificationClient instance;

		static ExposureNotificationClient Instance
			=> instance ??= new ExposureNotificationClient();

		static async Task PlatformStart(IExposureNotificationHandler handler)
		{
			var c = handler.Configuration;

			// TODO: Verify mapping of config
			await Instance.Start(new ExposureNotificationClient.ExposureConfiguration
			{
				AttenuationScores = c.AttenuationScores,
				DurationScores = c.DurationScores,
				DaysSinceLastExposureScores = c.DaysScores,
				TransmissionRiskScores = c.TransmissionRiskScores
			});
		}

		static Task PlatformStop()
			=> Instance.Stop();

		static Task<bool> PlatformIsEnabled()
			=> Instance.IsEnabled();

		// Gets the contact info of anyone the user had contact with who was diagnosed
		static async Task<IEnumerable<ExposureInfo>> PlatformGetExposureInformation()
		{
			var details = await Instance.GetExposureInformation();

			return details.Select(d => new ExposureInfo(
				DateTimeOffset.UnixEpoch.AddMilliseconds(d.DateMillisSinceEpoch).UtcDateTime,
				TimeSpan.FromMinutes(d.DurationMinutes),
				d.AttenuationValue,
				(byte)d.TotalRiskScore, // TODO: Check
				(RiskLevel)((int)d.TransmissionRiskLevel)));
		}

		// Call this when the user has confirmed diagnosis
		static async Task PlatformSubmitSelfDiagnosis()
		{
			var selfKeys = await Instance.GetTemporaryExposureKeyHistory();

			await Handler.UploadSelfExposureKeysToServer(
				selfKeys.Select(k => new TemporaryExposureKey(
					k.KeyData,
					(ulong)k.RollingStartNumber,
					TimeSpan.FromMinutes(k.RollingDuration),
					(RiskLevel)k.TransmissionRiskLevel)));
		}

		// Tells the local API when new diagnosis keys have been obtained from the server
		static async Task<ExposureDetectionSummary> PlatformAddDiagnosisKeys(IEnumerable<TemporaryExposureKey> diagnosisKeys)
		{
			var batchSize = await Instance.GetMaxDiagnosisKeys();
			var sequence = diagnosisKeys;

			while (sequence.Any())
			{
				var batch = sequence.Take(batchSize);
				sequence = sequence.Skip(batchSize);

				await Instance.ProvideDiagnosisKeys(
					batch.Select(k => new ExposureNotificationClient.TemporaryExposureKey
					{
						KeyData = k.KeyData,
						RollingStartNumber = (long)k.RollingStart,
						RollingDuration = (long)k.RollingDuration.TotalMinutes,
						TransmissionRiskLevel = (ExposureNotificationClient.RiskLevel)k.TransmissionRiskLevel
					}).ToList());
			}

			var summary = await Instance.GetExposureSummary();

			// TODO: Reevaluate byte usage here
			return new ExposureDetectionSummary(summary.DaysSinceLastExposure, (ulong)summary.MatchedKeyCount, (byte)summary.MaximumRiskScore);
		}

		static async Task<IEnumerable<TemporaryExposureKey>> PlatformGetTemporaryExposureKeys()
		{
			var exposureKeyHistory = await Instance.GetTemporaryExposureKeyHistory();
			
			return exposureKeyHistory.Select(k =>
				new TemporaryExposureKey(
					k.KeyData,
					(ulong)k.RollingStartNumber,
					TimeSpan.FromMinutes(k.RollingDuration * 10),
					(RiskLevel)((int)k.TransmissionRiskLevel)
					));
		}

		internal static async Task<ExposureDetectionSummary> AndroidGetExposureSummary()
		{
			var s = await Instance.GetExposureSummary();

			// TODO: Verify risk score byte 
			return new ExposureDetectionSummary(s.DaysSinceLastExposure, (ulong)s.MatchedKeyCount, (byte)s.MaximumRiskScore);
		}
	}
}
