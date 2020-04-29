using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExposureNotification
{
	public class TemporaryExposureKey
	{
		public byte[] KeyData { get; set; }

		public DateTime Timestamp { get; set; }
	}
}
