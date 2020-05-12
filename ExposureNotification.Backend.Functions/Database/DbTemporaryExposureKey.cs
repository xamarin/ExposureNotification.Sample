using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Google.Protobuf;
using Xamarin.ExposureNotifications;

namespace ExposureNotification.Backend
{
	class DbTemporaryExposureKey
	{
		public const string DefaultRegion = "default";

		[Key, Column(Order = 0)]
		public string Id { get; set; } = Guid.NewGuid().ToString();

		public bool Processed { get; set; } = false;

		public string Region { get; set; } = DefaultRegion;

		public string Base64KeyData { get; set; }

		public long TimestampMsSinceEpoch { get; set; }

		public long RollingStartSecondsSinceEpoch { get; set; }

		public int RollingDuration { get; set; }

		public int TransmissionRiskLevel { get; set; }

		public TemporaryExposureKey ToKey()
			=> new TemporaryExposureKey(
				Convert.FromBase64String(Base64KeyData),
				DateTimeOffset.FromUnixTimeSeconds(RollingStartSecondsSinceEpoch),
				TimeSpan.FromMinutes(RollingDuration),
				(RiskLevel)TransmissionRiskLevel);

		public static DbTemporaryExposureKey FromKey(TemporaryExposureKey key)
			=> new DbTemporaryExposureKey
			{
				Base64KeyData = Convert.ToBase64String(key.KeyData),
				TimestampMsSinceEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
				RollingStartSecondsSinceEpoch = key.RollingStart.ToUnixTimeSeconds(),
				RollingDuration = (int)key.RollingDuration.TotalMinutes,
				TransmissionRiskLevel = (int)key.TransmissionRiskLevel
			};

		public TemporaryExposureKeyBatchKey ToProtoKey()
			=> new TemporaryExposureKeyBatchKey
			{
				KeyData = ByteString.FromBase64(Base64KeyData),
				RollingStartNumber = (uint)RollingStartSecondsSinceEpoch,
				RollingPeriod = (uint)RollingDuration,
				TransmissionRiskLevel = TransmissionRiskLevel
			};
	}
}
