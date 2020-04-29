using System.Collections.Generic;

namespace Xamarin.ExposureNotifications
{
	public interface IExposureNotificationHandler
	{
		// App should send tracing keys to the backend server
		void ShouldSubmitTemporaryExposureKeys(List<TemporaryExposureKey> keys);

		// Contact was detected, user should be alerted!
		void OnContactsDetected(IEnumerable<ContactInfo> contacts);
	}
}
