using ExposureNotification.App.Views;
using Plugin.LocalNotification;
using Xamarin.Forms;

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
			NotificationCenter.Current.NotificationTapped += OnNotificationTapped;

			// Initialize the library which schedules background tasks, etc
			Xamarin.ExposureNotifications.ExposureNotification.Init();

			MainPage = new AppShell();
		}

		void OnNotificationTapped(NotificationTappedEventArgs e)
			=> Shell.Current?.GoToAsync($"//{nameof(ExposuresPage)}", false);

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
