﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace Mobile
{
	public class AppSettings
	{
		static AppSettings instance;

		public static AppSettings Instance
			=> instance ??= new AppSettings();

		public AppSettings()
		{
			var assembly = Assembly.GetExecutingAssembly();

			using var file = assembly.GetManifestResourceStream("Mobile.settings.json");
			using var sr = new StreamReader(file);
			var json = sr.ReadToEnd();
			var j = JObject.Parse(json);

			ApiUrlBase = j.Value<string>("apiUrlBase");
			BlobStorageUrlBase = j.Value<string>("blobStorageUrlBase");
			BlobStorageContainerNamePrefix = j.Value<string>("blobStorageContainerNamePrefix");
			SupportedRegions = j.Value<string>("supportedRegions").ToUpperInvariant().Split(';', ',', ':');
			AndroidSafetyNetApiKey = j.Value<string>("androidSafetyNetApiKey");
		}

		public string ApiUrlBase { get; }

		public string BlobStorageUrlBase { get; }

		public string[] SupportedRegions { get; }

		public string BlobStorageContainerNamePrefix { get; }

		public string AndroidSafetyNetApiKey { get; }

		internal Dictionary<string, ulong> GetDefaultDefaultBatch() =>
			Instance.SupportedRegions.ToDictionary(r => r, r => (ulong)0);
	}
}
