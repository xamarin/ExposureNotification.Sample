using System;
using System.Collections.Generic;
using System.Linq;
using Android.OS;
using Android.Runtime;
using System.Threading.Tasks;
using AndroidTask = Android.Gms.Tasks.Task;

namespace Android.Gms.ExposureNotifications
{
	public class ExposureNotificationClient
	{
		// Starts BLE broadcasts and scanning based on the defined protocol.
		//
		// If not previously used, this shows a user dialog for consent to start exposure
		// detection and get permission. Exposure configuration options can be provided to
		// tune the matching algorithm (for example, setting a signal strength attenuation
		// or duration threshold).
		//
		// Callbacks regarding exposure status and requesting diagnosis keys from the
		// server will be provided via a BroadcastReceiver.Clients should register
		// a receiver in their AndroidManifest which can handle the following actions:
		//
		// com.google.android.gms.exposurenotification.ACTION_EXPOSURE_STATE_UPDATED
		//
		// com.google.android.gms.exposurenotification.ACTION_REQUEST_DIAGNOSIS_KEYS
		//
		// This receiver should also be guarded by the
		//    com.google.android.gms.nearby.exposurenotification.EXPOSURE_CALLBACK
		// permission so that other apps are not able to fake this broadcast.
		public Task Start(ExposureConfiguration exposureConfiguration)
			=> CastTask(InternalStart(exposureConfiguration));

		// We will use metadata to mark this one internal in the binding
		internal AndroidTask InternalStart(ExposureConfiguration exposureConfiguration)
			=> null;

		public enum Status
		{
			Success = 0,
			FailedRejectedOptIn = 1,
			FailedServiceDisabled = 2,
			FailedBluetoothScanningDisabled = 3,
			FailedTemporarilyDisabled = 4,
			FailedInsufficentStorage = 5,
			FailedInternal = 6
		}

		// Exposure configuration parameters that can be provided when initializing the
		// service.
		//
		// These parameters are used to calculate risk for each exposure incident using
		// the following formula:
		//
		// <p><code>
		// RiskSum = (attenuationScore * attenuationWeight)
		// +(daysSinceLastExposureScore * daysSinceLastExposureWeight)
		// +(durationScore * durationWeight)
		// +(transmissionRiskScore * transmissionRiskWeight)
		// RiskScore = RiskSum
		// / (attenuationWeight
		// +daysSinceLastExposureWeight
		// +durationWeight
		// +transmissionRiskWeight)
		// </ code >
		//
		// <p> Scores are in the range 1-8. Weights are in the range 0-100. RiskScore is in
		// the range 1-8.
		public class ExposureConfiguration
		{
			// Minimum risk score.Excludes exposure incidents with scores lower than this.
			// Defaults to no minimum.
			public int MinimumRiskScore { get; set; }

			// Scores for attenuation buckets.Must contain 8 scores, one for each bucket
			// as defined below:
			//
			// <p><code>{
			//	@code
			// attenuationScores[0] when Attenuation > 73
			// attenuationScores[1] when 73 >= Attenuation > 63
			// attenuationScores[2] when 63 >= Attenuation > 51
			// attenuationScores[3] when 51 >= Attenuation > 33
			// attenuationScores[4] when 33 >= Attenuation > 27
			// attenuationScores[5] when 27 >= Attenuation > 15
			// attenuationScores[6] when 15 >= Attenuation > 10
			// attenuationScores[7] when 10 >= Attenuation
			// }</code>
			public int[] AttenuationScores { get; set; }


			// Weight to apply to the attenuation score. Must be in the range 0-100.
			public int AttenuationWeight { get; set; }

			// Scores for days since last exposure buckets.Must contain 8 scores, one for
			// each bucket as defined below:
			//
			// <p><code>{@code
			// daysSinceLastExposureScores[0] when Days >= 14
			// daysSinceLastExposureScores[1] when Days >= 12
			// daysSinceLastExposureScores[2] when Days >= 10
			// daysSinceLastExposureScores[3] when Days >= 8
			// daysSinceLastExposureScores[4] when Days >= 6
			// daysSinceLastExposureScores[5] when Days >= 4
			// daysSinceLastExposureScores[6] when Days >= 2
			// daysSinceLastExposureScores[7] when Days >= 0
			// }</code>
			public int[] DaysSinceLastExposureScores { get; set; }


			// Weight to apply to the days since last exposure score. Must be in the
			// range 0-100.
			public int DaysSinceLastExposureWeight { get; set; }

			// Scores for duration buckets.Must contain 8 scores, one for each bucket as
			// defined below:
			//
			// <p><code>{
			//	@code
			// durationScores[0] when Duration == 0
			// durationScores[1] when Duration <= 5
			// durationScores[2] when Duration <= 10
			// durationScores[3] when Duration <= 15
			// durationScores[4] when Duration <= 20
			// durationScores[5] when Duration <= 25
			// durationScores[6] when Duration <= 30
			// durationScores[7] when Duration > 30
			// }</code>
			public int[] DurationScores { get; set; }

			// Weight to apply to the duration score. Must be in the range 0-100.
			public double DurationWeight { get; set; }

			// Scores for transmission risk buckets.Must contain 8 scores, one for each
			// bucket as defined below:
			//
			// <p><code>{@code
			// transmissionRiskScores[0] when RISK_SCORE_LOWEST
			// transmissionRiskScores[1] when RISK_SCORE_LOW
			// transmissionRiskScores[2] when RISK_SCORE_LOW_MEDIUM
			// transmissionRiskScores[3] when RISK_SCORE_MEDIUM
			// transmissionRiskScores[4] when RISK_SCORE_MEDIUM_HIGH
			// transmissionRiskScores[5] when RISK_SCORE_HIGH
			// transmissionRiskScores[6] when RISK_SCORE_VERY_HIGH
			// transmissionRiskScores[7] when RISK_SCORE_HIGHEST
			//
			//}</code>
			public int[] TransmissionRiskScores { get; set; }

			// Weight to apply to the transmission risk score.Must be in the range 0-100.
			public double TransmissionRiskWeight { get; set; }
		}

		// Risk level defined for an {@link TemporaryExposureKey}.
		public enum RiskLevel
		{
			RiskLevelInvalid = 0,
			RiskLevelLowest = 1,
			RiskLevelLow = 2,
			RiskLEvelMedium = 3,
			RiskLevelMediumHigh = 4,
			RiskLevelHigh = 6,
			RiskLevelVeryHigh = 7,
			RiskLevelHighest = 8
		}

		// A key generated for advertising over a window of time.
		public class TemporaryExposureKey
		{
			/** The randomly generated Temporary Exposure Key information. */
			public byte[] KeyData { get; set; }

			// A number describing when a key starts.
			// It is equal to startTimeOfKeySinceEpochInSecs / (60 * 10).
			public long RollingStartNumber { get; set; }

			// A number describing how long a key is valid.
			// It is expressed in increments of 10 minutes(e.g. 144 for 24 hrs).
			public long RollingDuration { get; set; }

			// Risk of transmission associated with the person this key came from.
			public RiskLevel TransmissionRiskLevel { get; set; }
		}

		// Disables advertising and scanning.Contents of the database and keys will
		// remain.
		// If the client app has been uninstalled by the user, this will be automatically
		// invoked and the database and keys will be wiped from the device.
		public Task Stop()
			=> CastTask(InternalStop());

		internal AndroidTask InternalStop()
			=> null;

		// Indicates whether exposure notifications are currently running for the
		// requesting app.
		public async Task<bool> IsEnabled()
		{
			var r = await CastTask<Java.Lang.Boolean>(InternalIsEnabled());
			return r.BooleanValue();
		}

		public AndroidTask InternalIsEnabled()
			=> null;

		//
		// Gets {@link TemporaryExposureKey}
		//history to be stored on the server.
		//
		// This should only be done after proper verification is performed on the
		// client side that the user is diagnosed positive.
		//
		// The keys provided here will only be from previous days; keys will not be
		// released until after they are no longer an active exposure key.
		//
		// This shows a user permission dialog for sharing and uploading data to the
		// server.
		public async Task<List<TemporaryExposureKey>> GetTemporaryExposureKeyHistory()
		{
			var r = await CastTask<JavaList<TemporaryExposureKey>>(InternalGetTemporaryExposureKeyHistory());

			return r.ToList();
		}

		internal AndroidTask InternalGetTemporaryExposureKeyHistory()
			=> null;

		// Provides a list of diagnosis key files for exposure checking. The files are to
		// be synced from the server. Diagnosis keys older than the relevant period will be
		// ignored.
		//
		// When invoked after the
		// <code>com.google.android.gms.exposurenotification.ACTION_REQUEST_DIAGNOSIS_KEYS
		// </code> broadcast, this triggers a recalculation of exposure status which can be
		// obtained via {@link #getExposureSummary} after the calculation has finished.
		// When invoked outside of that action, diagnosis keys will still be stored and
		// matching will be performed in the near future.
		//
		// Should be called with a maximum of {@link #getMaxDiagnosisKeys()} keys at a
		// time, waiting until the Task finishes before providing the next batch.

		public Task ProvideDiagnosisKeys(List<TemporaryExposureKey> keys)
			=> CastTask(InternalProvideDiagnosisKeys(keys));

		internal AndroidTask InternalProvideDiagnosisKeys(List<TemporaryExposureKey> keys)
			=> null;

		// The maximum number of keys to pass into {@link #provideDiagnosisKeys} at any
		// given time.
		// int
		public async Task<int> GetMaxDiagnosisKeys()
		{
			var r = await CastTask<Java.Lang.Integer>(InternalGetMaxDiagnosisKeys());

			return r.IntValue();
		}

		internal AndroidTask InternalGetMaxDiagnosisKeys()
			=> null;


		public Task ProvideDiagnosisKeys(List<ParcelFileDescriptor> keyFiles)
			=> CastTask(InternalProvideDiagnosisKeys(keyFiles));

		internal AndroidTask InternalProvideDiagnosisKeys(List<ParcelFileDescriptor> keyFiles)
			=> null;

		// Gets a summary of the latest exposure calculation. The calculation happens
		// asynchronously, the most recent check’s result will be returned immediately.
		public Task<ExposureSummary> GetExposureSummary()
			=> CastTask<ExposureSummary>(InternalGetExposureSummary());

		internal AndroidTask InternalGetExposureSummary()
			=> null;

		// Summary information about recent exposures.
		// The client can get this information via {@link #getExposureSummary}.
		public class ExposureSummary : Java.Lang.Object
		{
			// Days since last match to a diagnosis key from the server. 0 is today, 1 is
			// yesterday, etc. Only valid if {@link #getMatchedKeysCount} > 0.
			public int DaysSinceLastExposure { get; set; }

			// Number of matched diagnosis keys.
			public int MatchedKeyCount { get; set; }

			// The highest risk score of all exposure incidents, it will be a value 1-8.
			public int MaximumRiskScore { get; set; }
		}

		// Gets detailed information about exposures that have occurred. The calculation
		// happens asynchronously, the most recent check’s result will be returned
		// immediately.
		//
		// When multiple {@link ExposureInformation} objects are returned, they can
		// be:
		// <ul>
		// <li>Multiple encounters with a single diagnosis key.
		// <li>Multiple encounters with the same device across key rotation boundaries.
		// <li>Encounters with multiple devices.
		// </ul>
		//
		// Plans to ensure user transparency in the use of this function are currently
		// being evaluated.
		public async Task<List<ExposureInformation>> GetExposureInformation()
		{
			var r = await CastTask<JavaList<ExposureInformation>>(InternalGetExposureInformation());
			return r.ToList();
		}

		internal AndroidTask InternalGetExposureInformation()
			=> null;

		// Information about an exposure, meaning a single diagnosis key
		// over a contiguous period of time specified by durationMinutes.
		// The client can get the exposure information via {@link #getExposureInformation}.
		public class ExposureInformation : Java.Lang.Object
		{
			// Day level resolution that the exposure occurred.
			public long DateMillisSinceEpoch { get; set; }

			// Length of exposure in 5 minute increments, with a 30 minute maximum.
			public int DurationMinutes { get; set; }

			// Signal strength attenuation, representing the closest the two devices were
			// within the duration of the exposure. This value is the advertiser's TX power
			// minus the receiver's maximum RSSI.
			// Note: This value may be misleading, higher attenuation does not necessarily
			// mean farther away. Phone in pocket vs hand can greatly affect this value,
			// along with other situations that can block the signal.
			// This value will be in the range 0-255.
			public int AttenuationValue { get; set; }

			// The transmission risk associated with the matched diagnosis key.
			public RiskLevel TransmissionRiskLevel { get; set; }

			// The total risk calculated for the exposure. See {@link ExposureConfiguration}
			// for more information about what is represented by the risk score.
			public int TotalRiskScore { get; set; }
		}

		// Delete all stored data associated with the user including exposure keys,
		// bluetooth scan history, and previously detected exposures.
		public Task ResetAllData()
			=> CastTask(InternalResetAllData());

		internal AndroidTask InternalResetAllData()
			=> null;

		public static Task CastTask(AndroidTask androidTask)
		{
			var tcs = new TaskCompletionSource<bool>();

			androidTask.AddOnCompleteListener(new MyCompleteListener(
				t =>
				{
					if (t.Exception == null)
						tcs.TrySetResult(false);
					else
						tcs.TrySetException(t.Exception);
				}
			));

			return tcs.Task;
		}

		public static Task<TResult> CastTask<TResult>(AndroidTask androidTask)
			where TResult : Java.Lang.Object
		{
			var tcs = new TaskCompletionSource<TResult>();

			androidTask.AddOnCompleteListener(new MyCompleteListener(
				t =>
				{
					if (t.Exception == null)
						tcs.TrySetResult(t.Result.JavaCast<TResult>());
					else
						tcs.TrySetException(t.Exception);
				}));

			return tcs.Task;
		}

		class MyCompleteListener : Java.Lang.Object, Android.Gms.Tasks.IOnCompleteListener
		{
			public MyCompleteListener(Action<AndroidTask> onComplete)
				=> OnCompleteHandler = onComplete;

			public Action<AndroidTask> OnCompleteHandler { get; }

			public void OnComplete(AndroidTask task)
				=> OnCompleteHandler?.Invoke(task);
		}
	}
}
