using Android.App;
using Android.Content;
using Android.Gms.ContactTracing;
using AndroidX.Core.App;
using Xamarin.Essentials;

namespace Xamarin.ExposureNotifications
{
	[Service(DirectBootAware = true, Permission = "android.permission.BIND_JOB_SERVICE")]
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
	internal class ExposureNotificationCallbackBroadcastReceiver : BroadcastReceiver
	{
		public override void OnReceive(Context context, Intent intent)
		{
			if (intent.Action == Intent.ActionBootCompleted)
				ExposureNotificationBootService.EnqueueWork(context, new Intent());
		}
	}

	[Service]
	internal class ExposureNotificationCallbackService : JobIntentService
	{
		const int JobId = 0x02;

		public static void EnqueueWork(Context context, Intent work)
			=> JobIntentService.EnqueueWork(context, Java.Lang.Class.FromType(typeof(ExposureNotificationCallbackService)), JobId, work);

		protected override void OnHandleWork(Intent workIntent)
		{

		}
	}
}
