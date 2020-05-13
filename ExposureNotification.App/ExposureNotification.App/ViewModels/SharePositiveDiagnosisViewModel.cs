using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Acr.UserDialogs;
using ExposureNotification.App.Resources;
using ExposureNotification.App.Services;
using Xamarin.Forms;

namespace ExposureNotification.App.ViewModels
{
	public class SharePositiveDiagnosisViewModel : BaseViewModel
	{
		public string DiagnosisUid { get; set; }

		public DateTime? DiagnosisTimestamp { get; set; } = DateTime.Now;

		public ICommand CancelCommand
			=> new Command(async () =>
				await Navigation.PopModalAsync(true));

		public ICommand SubmitDiagnosisCommand
			=> new Command(async () =>
			{
				using (UserDialogs.Instance.Loading("Verifying Diagnosis..."))
				{

					try
					{
						// Check the diagnosis is valid on the server before asking the native api's for the keys
						if (!await ExposureNotificationHandler.VerifyDiagnosisUid(DiagnosisUid))
							throw new Exception();
					}
					catch
					{
						await Device.InvokeOnMainThreadAsync(() => UserDialogs.Instance.HideLoading());
						await UserDialogs.Instance.AlertAsync("Your diagnosis cannot be verified at this time to be submitted.", "Verification Failed", "OK");
						return;
					}
				}

				using (UserDialogs.Instance.Loading("Submitting Diagnosis..."))
				{
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

						if (!DiagnosisTimestamp.HasValue || DiagnosisTimestamp.Value > DateTime.Now)
						{
							await UserDialogs.Instance.AlertAsync("Please provide a valid Test Date", "Invalid Test Date", "OK");
							return;
						}

						// Set the submitted UID
						LocalStateManager.Instance.AddDiagnosis(DiagnosisUid, new DateTimeOffset(DiagnosisTimestamp.Value));
						LocalStateManager.Save();

						// Submit our diagnosis
						await Xamarin.ExposureNotifications.ExposureNotification.SubmitSelfDiagnosisAsync();

						await UserDialogs.Instance.AlertAsync("Diagnosis Submitted", "Complete", "OK");

						await Navigation.PopModalAsync(true);
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex);

						await Device.InvokeOnMainThreadAsync(() => UserDialogs.Instance.HideLoading());
						UserDialogs.Instance.Alert("Please try again later.", "Failed", "OK");
					}
				}
			});
	}
}
