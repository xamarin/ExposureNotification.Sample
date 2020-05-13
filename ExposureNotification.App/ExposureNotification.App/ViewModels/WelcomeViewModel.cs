using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Acr.UserDialogs;
using ExposureNotification.App.Services;
using Xamarin.Forms;

namespace ExposureNotification.App.ViewModels
{
	public class WelcomeViewModel : BaseViewModel
	{
		public WelcomeViewModel()
		{
			Xamarin.ExposureNotifications.ExposureNotification.IsEnabledAsync()
				.ContinueWith(async t =>
				{
					if (t.Result)
					{
						IsEnabled = true;
						await Shell.Current.GoToAsync("//info");
					}
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

		public ICommand EnableCommand
			=> new Command(async () =>
			{
				using (UserDialogs.Instance.Loading(string.Empty))
				{
					var enabled = await Xamarin.ExposureNotifications.ExposureNotification.IsEnabledAsync();

					if (!enabled)
						await Xamarin.ExposureNotifications.ExposureNotification.StartAsync();
				}
				await Shell.Current.GoToAsync("//info");
			});

		public ICommand GetStartedCommand
			=> new Command(() => IsWelcomed = true);

		public ICommand NotNowCommand
			=> new Command(() => IsWelcomed = false);
	}
}
