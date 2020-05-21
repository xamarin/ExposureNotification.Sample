using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ExposureNotification.Backend.Proto;
using Google.Protobuf;

namespace ExposureNotification.Backend.Database
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

		public long TestDateMsSinceEpoch { get; set; }

		public long RollingStartSecondsSinceEpoch { get; set; }

		public int RollingDuration { get; set; }

		public int TransmissionRiskLevel { get; set; }

		public TemporaryExposureKey ToKey()
			=> new TemporaryExposureKey()
			{
				KeyData = ByteString.CopyFrom(Convert.FromBase64String(Base64KeyData)),
				RollingStartIntervalNumber = (int)RollingStartSecondsSinceEpoch,
				RollingPeriod = RollingDuration,
				TransmissionRiskLevel = TransmissionRiskLevel
			};

		public static DbTemporaryExposureKey FromKey(TemporaryExposureKey key, long testDateMsSinceEpoch)
			=> new DbTemporaryExposureKey
			{
				Base64KeyData = key.KeyData.ToBase64(),
				TimestampMsSinceEpoch = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
				TestDateMsSinceEpoch = testDateMsSinceEpoch,
				RollingStartSecondsSinceEpoch = key.RollingStartIntervalNumber,
				RollingDuration = key.RollingPeriod,
				TransmissionRiskLevel = key.TransmissionRiskLevel
			};
	}
}
