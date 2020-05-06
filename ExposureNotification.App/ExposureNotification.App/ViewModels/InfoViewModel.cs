using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace ExposureNotification.App.ViewModels
{
	public class InfoViewModel : BaseViewModel
	{

		internal const string welcomedPrefKey = "welcomed";

		public bool IsWelcomed
		{
			get => Preferences.Get(welcomedPrefKey, false);
			set
			{
				Preferences.Set(welcomedPrefKey, value);
				NotifyPropertyChanged(nameof(IsWelcomed));
			}
		}

		public ICommand EnableDisableCommand
			=> new Command(async () =>
			{
				var enabled = await Xamarin.ExposureNotifications.ExposureNotification.IsEnabledAsync();

				if (enabled)
					await Xamarin.ExposureNotifications.ExposureNotification.StopAsync();
				else
					await Xamarin.ExposureNotifications.ExposureNotification.StartAsync();
			});

		public ICommand GetStartedCommand
			=> new Command(() => IsWelcomed = true);

		public ICommand NotNowCommand
			=> new Command(() => IsWelcomed = false);
	}
}
