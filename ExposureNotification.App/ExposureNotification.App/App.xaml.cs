using System;
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

			MainPage = new AppShell();
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
