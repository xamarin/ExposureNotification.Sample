using System;

namespace Xamarin.ExposureNotifications
{
	public partial class TemporaryExposureKey
	{
		public TemporaryExposureKey(byte[] keyData, DateTimeOffset rollingStart, TimeSpan rollingDuration, RiskLevel transmissionRisk)
		{
			KeyDataBytes = keyData;
			RollingStart = rollingStart;
			RollingDuration = rollingDuration;
			TransmissionRiskLevelValue = transmissionRisk;
		}

		internal TemporaryExposureKey(byte[] keyData, long rollingStart, TimeSpan rollingDuration, RiskLevel transmissionRisk)
		{
			KeyDataBytes = keyData;
			RollingStart = DateTimeOffset.FromUnixTimeSeconds(rollingStart * (60 * 10));
			RollingDuration = rollingDuration;
			TransmissionRiskLevelValue = transmissionRisk;
		}

		public byte[] KeyDataBytes { get; set; }

		public DateTimeOffset RollingStart { get; set; }

		public TimeSpan RollingDuration { get; set; }

		public RiskLevel TransmissionRiskLevelValue { get; set; }
	}
}
