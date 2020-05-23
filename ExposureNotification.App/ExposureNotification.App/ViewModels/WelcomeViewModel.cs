using System.Windows.Input;
using Acr.UserDialogs;
using ExposureNotification.App.Services;
using ExposureNotification.App.Views;
using MvvmHelpers.Commands;
using Xamarin.Forms;
using Command = MvvmHelpers.Commands.Command;

namespace ExposureNotification.App.ViewModels
{
	public class WelcomeViewModel : ViewModelBase
	{
		public WelcomeViewModel()
		{
			Xamarin.ExposureNotifications.ExposureNotification.IsEnabledAsync()
				.ContinueWith(async t =>
				{
					if (t.Result)
					{
						IsEnabled = true;
						await GoToAsync($"//{nameof(InfoPage)}");
					}
				});
		}

		public new bool IsEnabled
		{
			get => LocalStateManager.Instance.LastIsEnabled;
			set
			{
				LocalStateManager.Instance.LastIsEnabled = value;
				LocalStateManager.Save();
				OnPropertyChanged();
			}
		}

		public bool IsWelcomed
		{
			get => LocalStateManager.Instance.IsWelcomed;
			set
			{
				LocalStateManager.Instance.IsWelcomed = value;
				LocalStateManager.Save();
				OnPropertyChanged();
			}
		}

		public AsyncCommand EnableCommand
			=> new AsyncCommand(async () =>
			{
				using (UserDialogs.Instance.Loading(string.Empty))
				{
					var enabled = await Xamarin.ExposureNotifications.ExposureNotification.IsEnabledAsync();

					if (!enabled)
						await Xamarin.ExposureNotifications.ExposureNotification.StartAsync();
				}
				await GoToAsync($"//{nameof(InfoPage)}");
			});

		public ICommand GetStartedCommand
			=> new Command(() => IsWelcomed = true);

		public ICommand NotNowCommand
			=> new Command(() => IsWelcomed = false);
	}
}
