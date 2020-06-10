using Acr.UserDialogs;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using Mobile.Styles;
using Plugin.LocalNotification;
using Xamarin.Forms.Platform.Android;

namespace Mobile.Droid
{
	[Activity(
		Label = "Exposure Notifications",
		Icon = "@mipmap/icon",
		Theme = "@style/MainTheme",
		MainLauncher = true,
		ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.UiMode | ConfigChanges.Orientation)]
	public class MainActivity : FormsAppCompatActivity
	{
		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			Xamarin.Essentials.Platform.Init(this, savedInstanceState);
			Xamarin.Forms.Forms.Init(this, savedInstanceState);
			Xamarin.Forms.FormsMaterial.Init(this, savedInstanceState);

			UserDialogs.Init(this);

			NotificationCenter.CreateNotificationChannel();

			LoadApplication(new App());

			NotificationCenter.NotifyNotificationTapped(base.Intent);
		}

		public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
		{
			Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

			base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
		}

		protected override void OnNewIntent(Intent intent)
		{
			NotificationCenter.NotifyNotificationTapped(intent);

			base.OnNewIntent(intent);
		}

		public override void OnConfigurationChanged(Configuration newConfig)
		{
			ThemeHelper.ChangeTheme();
			base.OnConfigurationChanged(newConfig);
		}

		protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
		{
			base.OnActivityResult(requestCode, resultCode, data);

			Xamarin.ExposureNotifications.ExposureNotification.OnActivityResult(requestCode, resultCode, data);
		}
	}
}
