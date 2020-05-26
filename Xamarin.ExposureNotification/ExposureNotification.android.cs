﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Gms.Nearby.ExposureNotification;

using Nearby = Android.Gms.Nearby.NearbyClass;
using AndroidRiskLevel = Android.Gms.Nearby.ExposureNotification.RiskLevel;
using AndroidX.Work;
using Android.Gms.Common.Apis;
using Android.Gms.Nearby.Connection;
using Android.Runtime;
using Java.Nio.FileNio;
using System.Reflection;
using Android.Bluetooth;

[assembly: UsesPermission(Android.Manifest.Permission.Bluetooth)]
[assembly: UsesPermission(Android.Manifest.Permission.AccessNetworkState)]

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
				.SetDurationAtAttenuationThresholds(c.DurationAtAttenuationThresholds)
				.Build();
		}

		const int requestCodeStartExposureNotification = 1111;
		const int requestCodeGetTempExposureKeyHistory = 2222;

		static TaskCompletionSource<object> tcsResolveConnection;

		public static void OnActivityResult(int requestCode, Result resultCode, global::Android.Content.Intent data)
		{
			if (requestCode == requestCodeStartExposureNotification || requestCode == requestCodeGetTempExposureKeyHistory)
			{
				if (resultCode == Result.Ok)
					tcsResolveConnection?.TrySetResult(null);
				else
					tcsResolveConnection.TrySetException(new AccessDeniedException("Failed to resolve Exposure Notifications API"));
			}
		}

		static PropertyInfo apiExceptionMStatusPropertyInfo = null;

		static async Task<T> ResolveApi<T>(int requestCode, Func<Task<T>> apiCall)
		{
			try
			{
				return await apiCall();
			}
			catch (ApiException apiEx)
			{
				if (apiEx.StatusCode == CommonStatusCodes.ResolutionRequired) // Resolution required
				{
					tcsResolveConnection = new TaskCompletionSource<object>();

					// Look up the property if it's null
					apiExceptionMStatusPropertyInfo ??=
						apiEx.GetType().GetProperty("MStatus", BindingFlags.Instance | BindingFlags.NonPublic);

					// Get the mStatus field from the java object using reflection since it's a protected property
					var val = (Java.Lang.Object)apiExceptionMStatusPropertyInfo.GetValue(apiEx);
					// Get the actual Statuses object back
					var statuses = val.JavaCast<Statuses>();

					// Start the resolution
					statuses.StartResolutionForResult(Essentials.Platform.CurrentActivity, requestCode);

					// Wait for the activity result to be called
					await tcsResolveConnection.Task;

					// Try the original api call again
					return await apiCall();
				}
			}

			return default;
		}

		static Task PlatformStart()
			=> ResolveApi<object>(requestCodeStartExposureNotification, async () =>
				{
					await Instance.StartAsync();
					return default;
				});

		static Task PlatformStop()
			=> ResolveApi<object>(requestCodeStartExposureNotification, async () =>
				{
					await Instance.StopAsync();
					return default;
				});

		static Task<bool> PlatformIsEnabled()
			=> ResolveApi(requestCodeStartExposureNotification, () =>
				Instance.IsEnabledAsync());

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

		static Task<IEnumerable<TemporaryExposureKey>> PlatformGetTemporaryExposureKeys()
			=> ResolveApi(requestCodeGetTempExposureKeyHistory, async () =>
				{
					var exposureKeyHistory = await Instance.GetTemporaryExposureKeyHistoryAsync();

					return exposureKeyHistory.Select(k =>
						new TemporaryExposureKey(
							k.GetKeyData(),
							k.RollingStartIntervalNumber,
							TimeSpan.Zero, // TODO: TimeSpan.FromMinutes(k.RollingDuration * 10),
							k.TransmissionRiskLevel.FromNative()));
				});

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

		static async Task<Status> PlatformGetStatusAsync()
		{
			var bt = BluetoothAdapter.DefaultAdapter;

			if (bt == null || !bt.IsEnabled)
				return Status.BluetoothOff;

			var status = await Instance.IsEnabledAsync();

			return status ? Status.Active : Status.Disabled;
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
