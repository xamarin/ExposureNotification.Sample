using System;
using Mobile.Resources;
using Mobile.Services;
using Mobile.Views;
using MvvmHelpers.Commands;
using Xamarin.Essentials;

namespace Mobile.ViewModels
{
	public class NotifyOthersViewModel : ViewModelBase
	{
		public NotifyOthersViewModel()
		{
			IsEnabled = true;
		}

		public bool DiagnosisPending
			=> (LocalStateManager.Instance.LatestDiagnosis?.DiagnosisDate ?? DateTimeOffset.MinValue)
				>= DateTimeOffset.UtcNow.AddDays(-14);

		public DateTimeOffset DiagnosisShareTimestamp
			=> LocalStateManager.Instance.LatestDiagnosis?.DiagnosisDate ?? DateTimeOffset.MinValue;

		public AsyncCommand SharePositiveDiagnosisCommand
			=> new AsyncCommand(() => GoToAsync($"{nameof(SharePositiveDiagnosisPage)}"));

		public AsyncCommand LearnMoreCommand
			=> new AsyncCommand(() => Browser.OpenAsync(Strings.NotifyOthers_LearnMore_Url));
	}
}
