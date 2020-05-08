using Android.App;
using Android.Content;
using Android.Runtime;
using AndroidX.Core.App;

namespace Xamarin.ExposureNotifications
{
	[BroadcastReceiver(Permission = "com.google.android.gms.nearby.exposurenotification.EXPOSURE_CALLBACK")]
	[IntentFilter(new[] { "com.google.android.gms.exposurenotification.ACTION_EXPOSURE_STATE_UPDATED" })]
	[Preserve]
	class ExposureNotificationCallbackBroadcastReceiver : BroadcastReceiver
	{
		public override void OnReceive(Context context, Intent intent)
			=> ExposureNotificationCallbackService.EnqueueWork(context, intent);
	}

	[Service]
	[Preserve]
	class ExposureNotificationCallbackService : JobIntentService
	{
		const int jobId = 0x02;

		public static void EnqueueWork(Context context, Intent work)
			=> EnqueueWork(context, Java.Lang.Class.FromType(typeof(ExposureNotificationCallbackService)), jobId, work);

		protected override async void OnHandleWork(Intent workIntent)
		{
			var summary = await ExposureNotification.PlatformGetExposureSummaryAsync();

			// Invoke the custom implementation handler code with the summary info
			if (summary?.MatchedKeyCount > 0)
			{
				var info = await ExposureNotification.PlatformGetExposureInformationAsync();

				await ExposureNotification.Handler.ExposureDetectedAsync(summary, info);
			}
		}
	}
}
