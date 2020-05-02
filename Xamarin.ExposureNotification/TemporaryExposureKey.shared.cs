using System;

namespace Xamarin.ExposureNotifications
{
	public class TemporaryExposureKey
	{
		public TemporaryExposureKey()
		{
		}

		public TemporaryExposureKey(byte[] keyData, DateTimeOffset rollingStart, TimeSpan rollingDuration, RiskLevel transmissionRisk)
		{
			KeyData = keyData;
			RollingStart = rollingStart;
			RollingDuration = rollingDuration;
			TransmissionRiskLevel = transmissionRisk;
		}

		internal TemporaryExposureKey(byte[] keyData, long rollingStart, TimeSpan rollingDuration, RiskLevel transmissionRisk)
		{
			KeyData = keyData;
			RollingStart = DateTimeOffset.FromUnixTimeSeconds(rollingStart * (60 * 10));
			RollingDuration = rollingDuration;
			TransmissionRiskLevel = transmissionRisk;
		}

		public byte[] KeyData { get; set; }

		public DateTimeOffset RollingStart { get; set; }

		public TimeSpan RollingDuration { get; set; }

		public RiskLevel TransmissionRiskLevel { get; set; }

		internal long RollingStartLong
			=> RollingStart.ToUnixTimeSeconds() / (60 * 10);
	}
}
