using System.Threading.Tasks;
using System.Windows.Input;
using Acr.UserDialogs;
using ExposureNotification.App.Services;
using ExposureNotification.App.Views;
using MvvmHelpers.Commands;
using Xamarin.Forms;

namespace ExposureNotification.App.ViewModels
{
	public class InfoViewModel : ViewModelBase
	{
		public InfoViewModel()
		{
			Xamarin.ExposureNotifications.ExposureNotification.IsEnabledAsync()
				.ContinueWith(async t =>
				{
					if (!t.Result)
						await Disabled();
				});
		}

		Task Disabled()
		{
			LocalStateManager.Instance.LastIsEnabled = false;
			LocalStateManager.Instance.IsWelcomed = false;
			LocalStateManager.Save();

			return GoToAsync($"//{nameof(WelcomePage)}");
		}

		public AsyncCommand DisableCommand
			=> new AsyncCommand(async () =>
			{
				try
				{
					using (UserDialogs.Instance.Loading(string.Empty))
					{
						var enabled = await Xamarin.ExposureNotifications.ExposureNotification.IsEnabledAsync();

						if (enabled)
							await Xamarin.ExposureNotifications.ExposureNotification.StopAsync();
					}
				}
				finally
				{
					await Disabled();
				}
			});
	}
}
