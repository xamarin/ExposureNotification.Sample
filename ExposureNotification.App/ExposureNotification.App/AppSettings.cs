using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Linq;

namespace ExposureNotification.App
{
	public class AppSettings
	{
		static AppSettings instance;

		public static AppSettings Instance
			=> instance ??= new AppSettings();

		public AppSettings()
		{
			var assembly = Assembly.GetExecutingAssembly();

			using var file = assembly.GetManifestResourceStream("ExposureNotification.App.settings.json");
			using var sr = new StreamReader(file);
			var json = sr.ReadToEnd();
			var j = JObject.Parse(json);

			ApiUrlBase = j.Value<string>("apiUrlBase");
			BlobStorageUrlBase = j.Value<string>("blobStorageUrlBase");
			BlobStorageContainerNamePrefix = j.Value<string>("blobStorageContainerNamePrefix");
			SupportedRegions = j.Value<string>("supportedRegions").Split(';', ',', ':');
			AndroidSafetyNetApiKey = j.Value<string>("androidSafetyNetApiKey");
		}

		public string ApiUrlBase { get; }

		public string BlobStorageUrlBase { get; }

		public string[] SupportedRegions { get; }

		public Dictionary<string, ulong> SupportedRegionsDefaultBatchNumbers
			=> SupportedRegions.ToDictionary(r => r, r => (ulong)0);

		public string BlobStorageContainerNamePrefix { get; }

		public string AndroidSafetyNetApiKey { get; }
	}
}
