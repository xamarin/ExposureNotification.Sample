using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExposureNotification.Backend.Database
{
	public class DbAuthorizedApp
	{
		[Key, Column(Order = 0)]
		public string Id { get; set; } = Guid.NewGuid().ToString();

		public string PackageName { get; set; }

		public string Platform { get; set; }

		public string[] AllowedRegions { get; set; }

		// SafetyNet configuration

		public string[] SafetyNetApkDigestSHA256 { get; set; }

		public bool SafetyNetBasicIntegrity { get; set; }

		public bool SafetyNetCTSProfileMatch { get; set; }

		public int SafetyNetPastTimeSeconds { get; set; }

		public int SafetyNetFutureTimeSeconds { get; set; }

		// DeviceCheck configuration

		public string DeviceCheckKeyId { get; set; }

		public string DeviceCheckTeamId { get; set; }

		public string DeviceCheckPrivateKey { get; set; }
	}
}
