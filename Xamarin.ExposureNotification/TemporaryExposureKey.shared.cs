using System;
using Google.Protobuf;

namespace Xamarin.ExposureNotifications
{
	public partial class TemporaryExposureKeyExport
	{
		public const int MaxKeysPerFile = 750000;
	}

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

		public byte[] KeyDataBytes
		{
			get => KeyData.ToByteArray();
			set => KeyData = ByteString.CopyFrom(value);
		}

		public DateTimeOffset RollingStart
		{
			get => DateTimeOffset.FromUnixTimeSeconds(RollingStartIntervalNumber);
			set => RollingStartIntervalNumber = (int)value.ToUnixTimeSeconds();
		}

		public TimeSpan RollingDuration
		{
			get => TimeSpan.FromMinutes(RollingPeriod);
			set => RollingPeriod = (int)value.TotalMinutes;
		}

		public RiskLevel TransmissionRiskLevelValue
		{
			get => (RiskLevel)TransmissionRiskLevel;
			set => TransmissionRiskLevel = (int)value;
		}
	}
}
