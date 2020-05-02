using System;

namespace Xamarin.ExposureNotifications
{
	public class TemporaryExposureKey
	{
		public TemporaryExposureKey()
		{
		}

		public TemporaryExposureKey(byte[] keyData, ulong rollingStart, TimeSpan rollingDuration, RiskLevel transmissionRisk)
		{
			KeyData = keyData;
			RollingStart = rollingStart;
			RollingDuration = rollingDuration;
			TransmissionRiskLevel = transmissionRisk;
		}

		public byte[] KeyData { get; set; }

		public ulong RollingStart { get; set; }

		public TimeSpan RollingDuration { get; set; }

		public RiskLevel TransmissionRiskLevel { get; set; }
	}
}
