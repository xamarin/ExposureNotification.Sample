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

		public DateTime Timestamp { get; set; }

		public ulong RollingStart { get; set; }

		public int RollingDuration { get; set; }

		public int TransmissionRiskLevel { get; set; }

		public TemporaryExposureKey ToKey()
			=> new TemporaryExposureKey(
				Convert.FromBase64String(Base64KeyData),
				RollingStart,
				TimeSpan.FromMinutes(RollingDuration),
				(RiskLevel)TransmissionRiskLevel);

		public static DbTemporaryExposureKey FromKey(TemporaryExposureKey key)
			=> new DbTemporaryExposureKey
			{
				Base64KeyData = Convert.ToBase64String(key.KeyData),
				Timestamp = DateTime.UtcNow,
				RollingStart = key.RollingStart,
				RollingDuration = (int)key.RollingDuration.TotalMinutes,
				TransmissionRiskLevel = (int)key.TransmissionRiskLevel
			};
	}
}
