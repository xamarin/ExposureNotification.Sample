using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Gms.Nearby.ExposureNotification;

using Nearby = Android.Gms.Nearby.NearbyClass;
using AndroidRiskLevel = Android.Gms.Nearby.ExposureNotification.RiskLevel;
using AndroidX.Work;

[assembly: UsesPermission(Android.Manifest.Permission.Bluetooth)]

namespace Xamarin.ExposureNotifications
{
	public static partial class ExposureNotification
	{
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

		public static void ConfigureBackgroundWorkRequest(TimeSpan repeatInterval, Action<PeriodicWorkRequest.Builder> requestBuilder)
		{
			if (requestBuilder == null)
				throw new ArgumentNullException(nameof(requestBuilder));
			if (repeatInterval == null)
				throw new ArgumentNullException(nameof(repeatInterval));

			bgRequestBuilder = requestBuilder;
			bgRepeatInterval = repeatInterval;
		}

		static Action<PeriodicWorkRequest.Builder> bgRequestBuilder = b =>
			b.SetConstraints(new Constraints.Builder()
				.SetRequiresBatteryNotLow(true)
				.SetRequiresDeviceIdle(true)
				.SetRequiredNetworkType(NetworkType.Connected)
				.Build());

		static TimeSpan bgRepeatInterval = TimeSpan.FromHours(6);

		static Task PlatformScheduleFetch()
		{
			var workManager = WorkManager.GetInstance(Essentials.Platform.AppContext);

			var workRequestBuilder = new PeriodicWorkRequest.Builder(
				typeof(BackgroundFetchWorker),
				bgRepeatInterval);

			bgRequestBuilder.Invoke(workRequestBuilder);
			
			var workRequest = workRequestBuilder.Build();

			workManager.EnqueueUniquePeriodicWork("exposurenotification",
				ExistingPeriodicWorkPolicy.Replace,
				workRequest);

			return Task.CompletedTask;
		}

		// Tells the local API when new diagnosis keys have been obtained from the server
		static async Task PlatformDetectExposuresAsync(IEnumerable<string> keyFiles)
		{
			var config = await GetConfigurationAsync();

			await Instance.ProvideDiagnosisKeysAsync(
				keyFiles.Select(f => new Java.IO.File(f)).ToList(),
				config,
				Guid.NewGuid().ToString());
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

	public class BackgroundFetchWorker : Worker
	{
		public BackgroundFetchWorker(global::Android.Content.Context context, WorkerParameters workerParameters)
			: base(context, workerParameters)
		{
		}

		public override Result DoWork()
		{
			try
			{
				Task.Run(() => DoAsyncWork()).GetAwaiter().GetResult();
				return Result.InvokeSuccess();
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex);
				return Result.InvokeRetry();
			}
		}

		async Task DoAsyncWork()
		{
			if (await ExposureNotification.IsEnabledAsync())
				await ExposureNotification.UpdateKeysFromServer();
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
