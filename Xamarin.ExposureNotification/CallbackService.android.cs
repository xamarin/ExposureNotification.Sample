using Android.App;
using Android.Content;
using Android.Gms.ExposureNotifications;
using AndroidX.Core.App;
using System;
using Xamarin.Essentials;

namespace Xamarin.ExposureNotifications
{
	[Service(
		DirectBootAware = true,
		Permission = "android.permission.BIND_JOB_SERVICE")]
	internal class ExposureNotificationBootService : JobIntentService
	{
		const int JobId = 0x01;

		public static void EnqueueWork(Context context, Intent work)
			=> JobIntentService.EnqueueWork(context, Java.Lang.Class.FromType(typeof(ExposureNotificationBootService)), JobId, work);

		protected override async void OnHandleWork(Intent workIntent)
		{
			if (Preferences.Get(ExposureNotification.Prefs_ExposureNotification_Enabled_Key, false))
				await ExposureNotification.Start();
		}
	}

	[BroadcastReceiver]
	[IntentFilter(new [] { Intent.ActionBootCompleted })]
	internal class ExposureNotificationBootBroadcastReceiver : BroadcastReceiver
	{
		public override void OnReceive(Context context, Intent intent)
		{
			if (intent.Action == Intent.ActionBootCompleted)
				ExposureNotificationBootService.EnqueueWork(context, new Intent());
		}
	}

	[BroadcastReceiver(Permission = "com.google.android.gms.nearby.exposurenotification.EXPOSURE_CALLBACK")]
	[IntentFilter(new[]
	{
		ActionExposureStateUpdated,
		ActionRequestDiagnosisKeys
	})]
	internal class ExposureNotificationCallbackBroadcastReceiver : BroadcastReceiver
	{
		internal const string ActionExposureStateUpdated = "com.google.android.gms.exposurenotification.ACTION_EXPOSURE_STATE_UPDATED";
		internal const string ActionRequestDiagnosisKeys = "com.google.android.gms.exposurenotification.ACTION_REQUEST_DIAGNOSIS_KEYS";

		public override void OnReceive(Context context, Intent intent)
			=> ExposureNotificationCallbackService.EnqueueWork(context, intent);
	}

	[Service]
	internal class ExposureNotificationCallbackService : JobIntentService
	{
		const int JobId = 0x02;

		public static void EnqueueWork(Context context, Intent work)
			=> JobIntentService.EnqueueWork(context, Java.Lang.Class.FromType(typeof(ExposureNotificationCallbackService)), JobId, work);

		protected override async void OnHandleWork(Intent workIntent)
		{
			var typeName = Preferences.Get(ExposureNotification.Prefs_ExposureNotification_Handler_Type_Key, string.Empty);

			if (string.IsNullOrEmpty(typeName))
				throw new NullReferenceException(nameof(typeName));

			var handlerInstance = (IExposureNotificationHandler)Activator.CreateInstance(Type.GetType(typeName));

			if (workIntent.Action == ExposureNotificationCallbackBroadcastReceiver.ActionExposureStateUpdated)
				await handlerInstance.ExposureStateUpdated(); // State updated check for exposure
			else if (workIntent.Action == ExposureNotificationCallbackBroadcastReceiver.ActionRequestDiagnosisKeys)
				await handlerInstance.RequestDiagnosisKeys(); //Send more keys to the server
		}
	}
}
