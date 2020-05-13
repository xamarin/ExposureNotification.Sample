using System.Windows.Input;
using ExposureNotification.App.Resources;
using ExposureNotification.App.Views;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace ExposureNotification.App.ViewModels
{
	public class NotifyOthersViewModel : BaseViewModel
	{
		public ICommand SharePositiveDiagnosisCommand
			=> new Command(() => Navigation.PushModalAsync(new NavigationPage(new SharePositiveDiagnosisPage())));

		public ICommand LearnMoreCommand
			=> new Command(() => Browser.OpenAsync(Strings.NotifyOthers_LearnMore_Url));
	}
}
