using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using ExposureNotification.App.Views;
using Xamarin.Forms;
using ExposureNotification.App.Resources;

namespace ExposureNotification.App.ViewModels
{
	public class NotifyOthersViewModel : BaseViewModel
	{
		public ICommand SharePositiveDiagnosisCommand
			=> new Command(async () =>
				await Navigation.PushModalAsync(
					new NavigationPage(new SharePositiveDiagnosisPage())));

		public ICommand LearnMoreCommand
			=> new Command(async () =>
				await Xamarin.Essentials.Browser.OpenAsync(Strings.NotifyOthers_LearnMore_Url));
	}
}
