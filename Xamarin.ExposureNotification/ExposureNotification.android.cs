using Android.AccessibilityServices;
using Android.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AndroidPendingIntent = global::Android.App.PendingIntent;
using Xamarin.Essentials;
using Android.Gms.ContactTracing;

namespace Xamarin.ExposureNotifications
{
	public static partial class ExposureNotification
	{
		static ContactTracing instance;

		static ContactTracing Instance
			=> instance ??= new ContactTracing();

		static async Task PlatformStart(IExposureNotificationHandler handler)
		{
			var context = Platform.AppContext;

			var callbackIntent = new Intent(context, typeof(ExposureNotificationCallbackService));

			var status = await Instance.StartContactTracing(AndroidPendingIntent.GetService(
				context,
				101,
				callbackIntent,
				global::Android.App.PendingIntentFlags.UpdateCurrent));

			if (status != Status.Success)
				throw new Exception();
		}

		static async Task PlatformStop()
		{
			var status = await Instance.StopContactTracing();

			if (status != Status.Success)
				throw new Exception();
		}

		static async Task<bool> PlatformIsEnabled()
			=> await Instance.IsContactTracingEnabled() == Status.Success;

		// Gets the contact info of anyone the user had contact with who was diagnosed
		static async Task<IEnumerable<ContactInfo>> PlatformGetContacts()
		{
			var contacts = await Instance.GetContactInformation();
			return contacts.Select(c => new ContactInfo(c.ContactDate, TimeSpan.FromMinutes(c.Duration)));
		}

		// Call this when the user has confirmed diagnosis
		static async Task PlatformSubmitPositiveDiagnosis()
		{
			var status = await Instance.StartSharingDailyTracingKeys();
			if (status != Status.Success)
				throw new Exception();
		}

		// Tells the local API when new diagnosis keys have been obtained from the server
		static async Task PlatformProcessDiagnosisKeys(IEnumerable<TemporaryExposureKey> diagnosisKeys)
		{
			var batchSize = Instance.MaxDiagnosisKeys;

			for (int i = 0; i < diagnosisKeys.Count(); i += batchSize)
			{
				var batch = diagnosisKeys.Skip(i).Take(batchSize)
					.Select(k => new global::Android.Gms.ContactTracing.DailyTracingKey(k.KeyData, k.Timestamp))
					.ToList();

				var status = await Instance.ProvideDiagnosisKeys(batch);
				if (status != Status.Success)
					throw new Exception();
			}
		}
	}
}
