using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace Xamarin.ExposureNotifications
{
	public static partial class ExposureNotification
	{
		internal const string Prefs_ExposureNotification_Enabled_Key = "ExposureNotification_enabled";
		internal const string Prefs_ExposureNotification_SubmittedDate_Key = "ExposureNotification_submitteddate";
		internal const string Prefs_ExposureNotification_Handler_Type_Key = "ExposureNotification_handler_type";

		public static Task Start<TExposureNotificationHandler>() where TExposureNotificationHandler : IExposureNotificationHandler
			=> StartWithTypeName(typeof(TExposureNotificationHandler).FullName);

		internal static Task Start()
			=> StartWithTypeName(null); // Get saved type name

		public static Task<bool> IsEnabledAsync()
			=> PlatformIsEnabled();

		public static bool LastEnabledState
			=> Preferences.Get(Prefs_ExposureNotification_Enabled_Key, false);

		static IExposureNotificationHandler GetCallbackHandlerInstance(string typeName = null)
		{
			if (string.IsNullOrEmpty(typeName))
				typeName = Preferences.Get(Prefs_ExposureNotification_Handler_Type_Key, (string)null);

			if (string.IsNullOrEmpty(typeName))
				throw new NullReferenceException(nameof(typeName));

			var handlerInstance = (IExposureNotificationHandler)Activator.CreateInstance(Type.GetType(typeName));

			Preferences.Set(Prefs_ExposureNotification_Handler_Type_Key, typeName);

			return handlerInstance;
		}

		static async Task StartWithTypeName(string typeName)
		{
			var handlerInstance = GetCallbackHandlerInstance(typeName);

			await PlatformStart(handlerInstance);

			Preferences.Set(Prefs_ExposureNotification_Enabled_Key, true);
		}

		public static async Task Stop()
		{
			await PlatformStop();

			Preferences.Set(Prefs_ExposureNotification_Handler_Type_Key, string.Empty);
			Preferences.Set(Prefs_ExposureNotification_Enabled_Key, false);
		}
		
		// Gets the contact info of anyone the user had contact with who was diagnosed
		public static Task<IEnumerable<ExposureInfo>> GetExposureInformation()
			=> PlatformGetExposureInformation();

		public static Task<ExposureDetectionSummary> GetExposureSummary()
			=> PlatformGetExposureSummary();

		// Call this when the user has confirmed diagnosis
		public static async Task SubmitPositiveDiagnosis()
		{
			await PlatformSubmitPositiveDiagnosis();

			Preferences.Set(Prefs_ExposureNotification_SubmittedDate_Key, DateTime.UtcNow);
		}

		public static bool HasSubmittedDiagnosis
			=> Preferences.Get(Prefs_ExposureNotification_SubmittedDate_Key, DateTime.UtcNow.AddYears(-1))
				>= DateTime.UtcNow.AddDays(-14);

		// Tells the local API when new diagnosis keys have been obtained from the server
		public static Task ProcessDiagnosisKeys(IEnumerable<TemporaryExposureKey> diagnosisKeys)
			=> PlatformProcessDiagnosisKeys(diagnosisKeys);

		public static Task<IEnumerable<TemporaryExposureKey>> GetTemporaryExposureKeys()
			=> PlatformGetTemporaryExposureKeys();
	}

	public class Configuration
	{
		// Minimum risk score required to record
		public int MinimumRiskScore { get; set; } = 1;

		public int[] TransmissionRiskScores { get; set; } = new int[] { 1, 1, 1, 1, 1, 1, 1, 1 };

		// Scores assigned to the attenuation of the BTLE signal of exposures
		// A > 73dBm, 73 <= A > 63, 63 <= A > 51, 51 <= A > 33, 33 <= A > 27, 27 <= A > 15, 15 <= A > 10, A <= 10
		public int[] AttenuationScores { get; set; } = new [] { 1, 2, 3, 4, 5, 6, 7, 8 };

		// Scores assigned to each length of exposure
		// < 5min, 5min, 10min, 15min, 20min, 25min, 30min, > 30min
		public int[] DurationScores { get; set; } = new[] { 1, 2, 3, 4, 5, 6, 7, 8 };

		// Scores assigned to each range of days of exposure
		// >= 14days, 13-12, 11-10, 9-8, 7-6, 5-4, 3-2, 1-0
		public int[] DaysScores { get; set; } = new[] { 8, 7, 6, 5, 4, 3, 2, 1 };
	}
}
