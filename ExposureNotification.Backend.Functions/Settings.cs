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
	}
}
