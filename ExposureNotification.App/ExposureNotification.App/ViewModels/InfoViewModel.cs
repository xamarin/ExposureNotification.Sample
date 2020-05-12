using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using ExposureNotification.App.Services;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace ExposureNotification.App.ViewModels
{
	public class InfoViewModel : BaseViewModel
	{
		public InfoViewModel()
		{
			Xamarin.ExposureNotifications.ExposureNotification.IsEnabledAsync()
				.ContinueWith(t =>
				{
					IsEnabled = t.Result;
				});
		}

		public bool IsEnabled
		{
			get => LocalStateManager.Instance.LastIsEnabled;
			set
			{
				LocalStateManager.Instance.LastIsEnabled = value;
				LocalStateManager.Save();
				NotifyPropertyChanged(nameof(IsEnabled));
			}
		}

		public bool IsWelcomed
		{
			get => LocalStateManager.Instance.IsWelcomed;
			set
			{
				LocalStateManager.Instance.IsWelcomed = value;
				LocalStateManager.Save();
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

				IsEnabled = enabled;
			});

		public ICommand GetStartedCommand
			=> new Command(() => IsWelcomed = true);

		public ICommand NotNowCommand
			=> new Command(() => IsWelcomed = false);
	}
}
