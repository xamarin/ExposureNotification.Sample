using System;
using System.ComponentModel.DataAnnotations;
using Xamarin.ExposureNotifications;

namespace ExposureNotification.Backend
{
	class DbTemporaryExposureKey
	{
		[Key]
		public string Id { get; set; } = Guid.NewGuid().ToString();

		public string Base64KeyData { get; set; }

		public long TimestampSecondsSinceEpoch { get; set; }

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
				TimestampSecondsSinceEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
				RollingStartSecondsSinceEpoch = key.RollingStart.ToUnixTimeSeconds(),
				RollingDuration = (int)key.RollingDuration.TotalMinutes,
				TransmissionRiskLevel = (int)key.TransmissionRiskLevel
			};
	}
}
