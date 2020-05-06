using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;

namespace Xamarin.ExposureNotifications
{
	// TODO: Verify if this is still needed
	// Previously the spec suggested starting the API needed to be done each boot
	// but it looks like in the latest revision this is not the case

	//[Service(
	//	DirectBootAware = true,
	//	Permission = "android.permission.BIND_JOB_SERVICE")]
	//internal class ExposureNotificationBootService : JobIntentService
	//{
	//	const int JobId = 0x01;

	//	public static void EnqueueWork(Context context, Intent work)
	//		=> JobIntentService.EnqueueWork(context, Java.Lang.Class.FromType(typeof(ExposureNotificationBootService)), JobId, work);

	//	protected override async void OnHandleWork(Intent workIntent)
	//		=> await ExposureNotification.StartAsync();
	//}

	//[BroadcastReceiver]
	//[IntentFilter(new [] { Intent.ActionBootCompleted })]
	//internal class ExposureNotificationBootBroadcastReceiver : BroadcastReceiver
	//{
	//	public override void OnReceive(Context context, Intent intent)
	//	{
	//		if (intent.Action == Intent.ActionBootCompleted)
	//			ExposureNotificationBootService.EnqueueWork(context, new Intent());
	//	}
	//}

	[BroadcastReceiver(Permission = "com.google.android.gms.nearby.exposurenotification.EXPOSURE_CALLBACK")]
	[IntentFilter(new[]
	{
		ActionExposureStateUpdated,
		ActionRequestDiagnosisKeys
	})]
	[Preserve]
	internal class ExposureNotificationCallbackBroadcastReceiver : BroadcastReceiver
	{
		internal const string ActionExposureStateUpdated = "com.google.android.gms.exposurenotification.ACTION_EXPOSURE_STATE_UPDATED";
		internal const string ActionRequestDiagnosisKeys = "com.google.android.gms.exposurenotification.ACTION_REQUEST_DIAGNOSIS_KEYS";

		public override void OnReceive(Context context, Intent intent)
			=> ExposureNotificationCallbackService.EnqueueWork(context, intent);
	}

	[Service]
	[Preserve]
	internal class ExposureNotificationCallbackService : JobIntentService
	{
		const int JobId = 0x02;

		public static void EnqueueWork(Context context, Intent work)
			=> JobIntentService.EnqueueWork(context, Java.Lang.Class.FromType(typeof(ExposureNotificationCallbackService)), JobId, work);

		protected override async void OnHandleWork(Intent workIntent)
		{
			if (workIntent.Action == ExposureNotificationCallbackBroadcastReceiver.ActionExposureStateUpdated)
			{
				var summary = await ExposureNotification.AndroidGetExposureSummary();

				if (summary != null && summary.MatchedKeyCount > 0)
				{
					// Invoke the custom implementation handler code with the summary info
					await ExposureNotification.Handler.ExposureDetected(
						summary,
						() => ExposureNotification.GetExposureInformationAsync());
				}
			}
			else if (workIntent.Action == ExposureNotificationCallbackBroadcastReceiver.ActionRequestDiagnosisKeys)
			{
				// Go fetch latest keys from server
				await ExposureNotification.UpdateKeysFromServer();
			}
		}
	}
}
