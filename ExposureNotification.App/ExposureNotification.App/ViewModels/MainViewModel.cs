using Acr.UserDialogs;
using ExposureNotification.App;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace ContactTracing.App.ViewModels
{
	public class MainViewModel : BaseViewModel
	{
		public MainViewModel()
		{
			IsEnabled = Xamarin.ExposureNotifications.ExposureNotification.LastEnabledState;
			NotifyPropertyChanged(nameof(IsEnabled));

			Xamarin.ExposureNotifications.ExposureNotification.IsEnabledAsync()
				.ContinueWith(t =>
				{
					IsEnabled = t.Result;
					NotifyPropertyChanged(nameof(IsEnabled));
				});
		}

		public bool IsEnabled { get; set; }

		public string EnableDisableText
			=> IsEnabled ? "Disable" : "Enable";

		public string DiagnosisUid { get; set; }

		public bool HasSubmittedDiagnosis
			=> Xamarin.ExposureNotifications.ExposureNotification.HasSubmittedDiagnosis;

		public ICommand EnableDisableCommand
			=> new Command(async () =>
			{
				var enabled = await Xamarin.ExposureNotifications.ExposureNotification.IsEnabledAsync();

				if (enabled)
					await Xamarin.ExposureNotifications.ExposureNotification.Stop();
				else
					await Xamarin.ExposureNotifications.ExposureNotification.Start<ExposureNotificationHandler>();
			});

		public ICommand SubmitDiagnosisCommand
			=> new Command(async () =>
			{
				UserDialogs.Instance.Loading("Submitting Diagnosis...");

				try
				{
					var enabled = await Xamarin.ExposureNotifications.ExposureNotification.IsEnabledAsync();

					if (!enabled)
						return;

					if (string.IsNullOrEmpty(DiagnosisUid))
					{
						await UserDialogs.Instance.AlertAsync("Please provide a Diagnosis Identifier", "Diagnosis Identifier Required", "OK");
						return;
					}

					await Xamarin.ExposureNotifications.ExposureNotification.SubmitPositiveDiagnosis();

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
