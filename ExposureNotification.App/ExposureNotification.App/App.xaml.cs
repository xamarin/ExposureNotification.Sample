using System;
using Plugin.LocalNotification;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ExposureNotification.App
{
	public partial class App : Application
	{
		public App()
		{
			InitializeComponent();

#if DEBUG
			// For debug mode, set the mock api provider to interact
			// with some fake data
			Xamarin.ExposureNotifications.ExposureNotification.OverrideNativeImplementation(
				new Services.TestNativeImplementation());
#endif
			// Local Notification tap event listener
			NotificationCenter.Current.NotificationTapped += Current_NotificationTapped; ;

			MainPage = new AppShell();
		}

		private void Current_NotificationTapped(NotificationTappedEventArgs e)
		{
		}

		protected override void OnStart()
		{
		}

		protected override void OnSleep()
		{
		}

		protected override void OnResume()
		{
		}
	}
}
