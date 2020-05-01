using Acr.UserDialogs;
using ExposureNotification.App;
using ExposureNotification.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.ExposureNotifications;
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

						X509Certificate2 cert = null;

						using (var s = Assembly.GetCallingAssembly().GetManifestResourceStream(Config.CertificateResourceFilename))
						using (var m = new MemoryStream())
						{
							await s.CopyToAsync(m);
							m.Position = 0;

							cert = new X509Certificate2(m.ToArray());
						}

						var encoder = new DefaultTemporaryExposureKeyEncoder(cert);

						var url = $"{Config.ApiUrlBase.TrimEnd('/')}/diagnosis";

						var encodedKeys = tempExposureKeys.Select(k => new TemporaryExposureKey
						{
							KeyData = encoder.Encode(k.KeyData),
							RollingStart = k.RollingStart,
							RollingDuration = k.RollingDuration,
							TransmissionRiskLevel = k.TransmissionRiskLevel
						});

						var json = JsonConvert.SerializeObject(new DiagnosisSubmission
						{
							DiagnosisUid = diagnosisUid,
							TemporaryExposureKeys = encodedKeys.ToList()
						});

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
