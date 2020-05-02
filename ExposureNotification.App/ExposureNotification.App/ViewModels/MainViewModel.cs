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
		const string PrefsDiagnosisSubmissionDate = "prefs_diagnosis_submit_date";
		const string PrefsDiagnosisSubmissionUid = "prefs_diagnosis_submit_uid";

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
			=> Preferences.Get(PrefsDiagnosisSubmissionDate, DateTime.MinValue)
				>= DateTime.UtcNow.AddDays(-14);

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
						return;

					if (string.IsNullOrEmpty(DiagnosisUid))
					{
						await UserDialogs.Instance.AlertAsync("Please provide a Diagnosis Identifier", "Diagnosis Identifier Required", "OK");
						return;
					}
					
					// Submit our diagnosis
					await Xamarin.ExposureNotifications.ExposureNotification.SubmitSelfDiagnosisAsync(async tempExposureKeys =>
					{
						var diagnosisUid = DiagnosisUid;

						if (string.IsNullOrEmpty(diagnosisUid))
							throw new InvalidOperationException();

						var url = $"{Config.ApiUrlBase.TrimEnd('/')}/diagnosis";

						var json = JsonConvert.SerializeObject((diagnosisUid, tempExposureKeys));

						var http = new HttpClient();
						var response = await http.PostAsync(url, new StringContent(json));

						response.EnsureSuccessStatusCode();

						Preferences.Set(PrefsDiagnosisSubmissionDate, DateTime.UtcNow);
						Preferences.Set(PrefsDiagnosisSubmissionUid, diagnosisUid);
					});

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
