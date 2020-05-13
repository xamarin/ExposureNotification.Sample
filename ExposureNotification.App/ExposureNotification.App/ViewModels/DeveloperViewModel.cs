using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Acr.UserDialogs;
using ExposureNotification.App.Services;
using Xamarin.Forms;

namespace ExposureNotification.App.ViewModels
{
	public class DeveloperViewModel : BaseViewModel
	{
		public string NativeImplementationName
			=> Xamarin.ExposureNotifications.ExposureNotification.OverridesNativeImplementation
				? "TEST" : "LIVE";

		public ICommand ResetSelfDiagnosis
			=> new Command(async () =>
			{
				LocalStateManager.Instance.ClearDiagnosis();
				LocalStateManager.Save();
				await UserDialogs.Instance.AlertAsync("Self Diagnosis Cleared!");
			});


		public ICommand ResetExposures
			=> new Command(async () =>
			{
				LocalStateManager.Instance.ExposureInformation.Clear();
				LocalStateManager.Instance.ExposureSummary = null;
				LocalStateManager.Save();
				await UserDialogs.Instance.AlertAsync("Exposures Cleared!");
			});

		public ICommand AddExposures
			=> new Command(async () =>
			{
				await Device.InvokeOnMainThreadAsync(() =>
				{
					LocalStateManager.Instance.ExposureInformation.Add(
						new Xamarin.ExposureNotifications.ExposureInfo(DateTime.Now.AddDays(-7), TimeSpan.FromMinutes(30), 70, 6, Xamarin.ExposureNotifications.RiskLevel.High));
					LocalStateManager.Instance.ExposureInformation.Add(
						new Xamarin.ExposureNotifications.ExposureInfo(DateTime.Now.AddDays(-3), TimeSpan.FromMinutes(10), 40, 3, Xamarin.ExposureNotifications.RiskLevel.Low));

					LocalStateManager.Save();
				});
			});

		public ICommand ResetWelcome
			=> new Command(async () =>
			{
				LocalStateManager.Instance.IsWelcomed = false;
				LocalStateManager.Save();
				await UserDialogs.Instance.AlertAsync("Welcome state reset!");
			});

		public ICommand ResetEnabled
			=> new Command(async () =>
			{
				using (UserDialogs.Instance.Loading(string.Empty))
				{
					if (await Xamarin.ExposureNotifications.ExposureNotification.IsEnabledAsync())
						await Xamarin.ExposureNotifications.ExposureNotification.StopAsync();

					LocalStateManager.Instance.LastIsEnabled = false;
					LocalStateManager.Save();
				}
				await UserDialogs.Instance.AlertAsync("Last known enabled state reset!");
			});

	}
}
