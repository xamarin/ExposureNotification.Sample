namespace ExposureNotification.Backend.Functions
{
	public class Settings
	{
		public string DbConnectionString { get; set; }

		public string BlobStorageConnectionString { get; set; }

		public string BlobStorageContainerNamePrefix { get; set; }

		public string[] SupportedRegions { get; set; }

		public bool DeleteKeysFromDbAfterBatching { get; set; }

		public bool DisableDeviceVerification { get; set; }

		public string SigningKeyBase64String { get; set; }
		public string VerificationKeyId { get; set; }
		public string VerificationKeyVersion { get; set; }

		public string AndroidPackageName { get; set; }
		public string iOSBundleId { get; set; }
		public string iOSDeviceCheckKeyId { get; set; }
		public string iOSDeviceCheckTeamId { get; set; }
		public string iOSDeviceCheckPrivateKey { get; set; }

	}
}
