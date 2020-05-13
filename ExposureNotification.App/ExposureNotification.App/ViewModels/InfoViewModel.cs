using System.Threading.Tasks;
using System.Windows.Input;
using Acr.UserDialogs;
using ExposureNotification.App.Services;
using Xamarin.Forms;

namespace ExposureNotification.App.ViewModels
{
	public class InfoViewModel : BaseViewModel
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

		async Task Disabled()
		{
			LocalStateManager.Instance.LastIsEnabled = false;
			LocalStateManager.Instance.IsWelcomed = false;
			LocalStateManager.Save();

			await Shell.Current.GoToAsync("//welcome");
		}

		public ICommand DisableCommand
			=> new Command(async () =>
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
