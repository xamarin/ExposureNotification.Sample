using Acr.UserDialogs;
using ExposureNotification.App;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace ContactTracing.App.ViewModels
{
	public class MainViewModel : BaseViewModel
	{
		public MainViewModel()
		{
			Xamarin.ExposureNotifications.ExposureNotification.IsEnabledAsync()
				.ContinueWith(t =>
				{
					IsEnabled = t.Result;
					NotifyPropertyChanged(nameof(IsEnabled));
				});
		}

		public bool IsEnabled { get; set; } = false;

		public string EnableDisableText
			=> IsEnabled ? "Disable" : "Enable";

		public string DiagnosisUid { get; set; }

		public bool HasSubmittedDiagnosis
			=> ExposureNotificationHandler.HasSubmittedDiagnosis;

		public ICommand EnableDisableCommand
			=> new Command(async () =>
			{
				var enabled = await Xamarin.ExposureNotifications.ExposureNotification.IsEnabledAsync();

				if (enabled)
					await Xamarin.ExposureNotifications.ExposureNotification.StopAsync();
				else
					await Xamarin.ExposureNotifications.ExposureNotification.StartAsync();
			});

		public ICommand SubmitDiagnosisCommand
			=> new Command(async () =>
			{
				UserDialogs.Instance.Loading("Submitting Diagnosis...");

				try
				{
					var enabled = await Xamarin.ExposureNotifications.ExposureNotification.IsEnabledAsync();

					if (!enabled)
					{
						await UserDialogs.Instance.AlertAsync("Please enable Exposure Notifications before submitting a diagnosis.", "Exposure Notifications Disabled", "OK");
						return;
					}

					if (string.IsNullOrEmpty(DiagnosisUid))
					{
						await UserDialogs.Instance.AlertAsync("Please provide a Diagnosis Identifier", "Diagnosis Identifier Required", "OK");
						return;
					}

					// Set the submitted UID
					ExposureNotificationHandler.DiagnosisUid = DiagnosisUid;

					// Submit our diagnosis
					await Xamarin.ExposureNotifications.ExposureNotification.SubmitSelfDiagnosisAsync();

					NotifyPropertyChanged(nameof(HasSubmittedDiagnosis));

					UserDialogs.Instance.HideLoading();
					await UserDialogs.Instance.AlertAsync("Diagnosis Submitted", "Complete", "OK");
				}
				catch
				{
					UserDialogs.Instance.HideLoading();
					UserDialogs.Instance.Alert("Please try again later.", "Failed", "OK");
				}
			});
	}
}
