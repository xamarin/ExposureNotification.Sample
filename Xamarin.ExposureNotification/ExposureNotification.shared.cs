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
			=> StartWithTypeName(Preferences.Get(Prefs_ExposureNotification_Handler_Type_Key, string.Empty));

		public static Task<bool> IsEnabledAsync()
			=> PlatformIsEnabled();

		public static bool LastEnabledState
			=> Preferences.Get(Prefs_ExposureNotification_Enabled_Key, false);

		static async Task StartWithTypeName(string typeName)
		{
			if (string.IsNullOrEmpty(typeName))
				throw new NullReferenceException(nameof(typeName));

			var handlerInstance = (IExposureNotificationHandler)Activator.CreateInstance(Type.GetType(typeName));

			await PlatformStart(handlerInstance);

			Preferences.Set(Prefs_ExposureNotification_Handler_Type_Key, typeName);
			Preferences.Set(Prefs_ExposureNotification_Enabled_Key, true);
		}

		public static async Task Stop()
		{
			await PlatformStop();

			Preferences.Set(Prefs_ExposureNotification_Handler_Type_Key, string.Empty);
			Preferences.Set(Prefs_ExposureNotification_Enabled_Key, false);
		}
		
		// Gets the contact info of anyone the user had contact with who was diagnosed
		public static Task<IEnumerable<ContactInfo>> GetContacts()
			=> PlatformGetContacts();

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
	}
}
