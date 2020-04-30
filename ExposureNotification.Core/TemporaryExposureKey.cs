using Newtonsoft.Json;
using System;

namespace ExposureNotification.Core
{
	public class TemporaryExposureKey
	{
		[JsonProperty("keyData")]
		public byte[] KeyData { get; set; }

		[JsonProperty("rollingStart")]
		public ulong RollingStart { get; set; }

		[JsonProperty("rollingDuration")]
		public int RollingDuration { get; set; }

		[JsonProperty("transmissionRiskLevel")]
		public int TransmissionRiskLevel { get; set; }
	}
}
