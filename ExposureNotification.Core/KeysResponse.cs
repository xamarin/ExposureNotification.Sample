using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.ExposureNotifications;

namespace ExposureNotification.Core
{
	public class KeysResponse
	{
		[JsonProperty("timestamp")]
		public DateTime Timestamp { get; set; }

		[JsonProperty("keys")]
		public List<TemporaryExposureKey> Keys { get; set; } = new List<TemporaryExposureKey>();
	}
}
