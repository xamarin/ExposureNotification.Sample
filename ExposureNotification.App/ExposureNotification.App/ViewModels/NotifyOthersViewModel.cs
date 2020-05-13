using System;
using System.Windows.Input;
using ExposureNotification.App.Resources;
using ExposureNotification.App.Services;
using ExposureNotification.App.Views;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace ExposureNotification.App.ViewModels
{
	public class NotifyOthersViewModel : BaseViewModel
	{

		public bool DiagnosisPending
			=> (LocalStateManager.Instance.LatestDiagnosis?.DiagnosisDate ?? DateTimeOffset.MinValue)
				>= DateTimeOffset.UtcNow.AddDays(-14);

		public DateTimeOffset DiagnosisShareTimestamp
			=> LocalStateManager.Instance.LatestDiagnosis?.DiagnosisDate ?? DateTimeOffset.MinValue;

		public ICommand SharePositiveDiagnosisCommand
			=> new Command(() => Navigation.PushModalAsync(new NavigationPage(new SharePositiveDiagnosisPage())));

		public ICommand LearnMoreCommand
			=> new Command(() => Browser.OpenAsync(Strings.NotifyOthers_LearnMore_Url));
	}
}
