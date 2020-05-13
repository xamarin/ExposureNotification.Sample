using System;
using System.Windows.Input;
using Acr.UserDialogs;
using ExposureNotification.App.Services;
using Xamarin.Forms;

namespace ExposureNotification.App.ViewModels
{
	public class SharePositiveDiagnosisViewModel : BaseViewModel
	{
		public string DiagnosisUid { get; set; }

		public DateTime? DiagnosisTimestamp { get; set; }

		public ICommand CancelCommand
			=> new Command(() => Navigation.PopModalAsync(true));

		public ICommand SubmitDiagnosisCommand
			=> new Command(async () =>
			{
				using var dialog = UserDialogs.Instance.Loading("Verifying Diagnosis...");

				try
				{
					// Check the diagnosis is valid on the server before asking the native api's for the keys
					if (!await ExposureNotificationHandler.VerifyDiagnosisUid(DiagnosisUid))
						throw new Exception();
				}
				catch
				{
					dialog.Hide();

					await UserDialogs.Instance.AlertAsync("Your diagnosis cannot be verified at this time to be submitted.", "Verification Failed", "OK");
					return;
				}

				dialog.Title = "Submitting Diagnosis...";

				try
				{
					var enabled = await Xamarin.ExposureNotifications.ExposureNotification.IsEnabledAsync();

					if (!enabled)
					{
						dialog.Hide();

						await UserDialogs.Instance.AlertAsync("Please enable Exposure Notifications before submitting a diagnosis.", "Exposure Notifications Disabled", "OK");
						return;
					}

					if (string.IsNullOrEmpty(DiagnosisUid))
					{
						dialog.Hide();

						await UserDialogs.Instance.AlertAsync("Please provide a Diagnosis Identifier", "Diagnosis Identifier Required", "OK");
						return;
					}

					// Set the submitted UID
					LocalStateManager.Instance.LatestDiagnosis.DiagnosisUid = DiagnosisUid;
					LocalStateManager.Instance.LatestDiagnosis.DiagnosisDate = DiagnosisTimestamp ?? DateTime.UtcNow;
					LocalStateManager.Save();

					// Submit our diagnosis
					await Xamarin.ExposureNotifications.ExposureNotification.SubmitSelfDiagnosisAsync();

					dialog.Hide();
					await UserDialogs.Instance.AlertAsync("Diagnosis Submitted", "Complete", "OK");

					await Navigation.PopModalAsync(true);
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);

					dialog.Hide();
					UserDialogs.Instance.Alert("Please try again later.", "Failed", "OK");
				}
			});
	}
}
