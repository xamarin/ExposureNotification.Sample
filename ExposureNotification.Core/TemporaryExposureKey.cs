using Newtonsoft.Json;
using System;

namespace ExposureNotification
{
	public class TemporaryExposureKey
	{
		[JsonProperty("keyData")]
		public byte[] KeyData { get; set; }

		[JsonProperty("timestamp")]
		public DateTime Timestamp { get; set; }
	}
}
