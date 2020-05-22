using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExposureNotification.Backend.Database
{
	public class DbSignerInfo
	{
		[Key, Column(Order = 0)]
		public string Id { get; set; } = Guid.NewGuid().ToString();

		public string AndroidPackage { get; set; }

		public string AppBundleId { get; set; }

		public string VerificationKeyId { get; set; }

		public string VerificationKeyVersion { get; set; }

		public string SigningKeyBase64String { get; set; }
	}
}
