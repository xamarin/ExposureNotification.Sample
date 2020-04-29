using System;

namespace Xamarin.ExposureNotifications
{
	public class ContactInfo
	{
		public ContactInfo(DateTime timestamp, TimeSpan duration)
		{
			Timestamp = timestamp;
			Duration = duration;
		}

		// When the contact occurred
		public DateTime Timestamp { get; }

		// How long the contact lasted in 5 min increments
		public TimeSpan Duration { get; }
	}
}
