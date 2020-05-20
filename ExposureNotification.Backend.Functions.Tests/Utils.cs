using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace ExposureNotification.Backend.Functions.Tests
{
	public static class Utils
	{
		public static List<TemporaryExposureKey> GenerateTemporaryExposureKeys(int daysBack)
		{
			var random = new Random();
			var nowDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 0, 0, 0, 0, DateTimeKind.Utc);

			var tracingKeys = new List<TemporaryExposureKey>();

			for (var day = nowDate.AddDays(-1 * daysBack); day <= nowDate; day += TimeSpan.FromDays(1))
			{
				for (var seg = nowDate; seg < nowDate.AddDays(1); seg += TimeSpan.FromMinutes(15))
				{
					var rnd = new byte[16];
					random.NextBytes(rnd);
					var duration = TimeSpan.FromMinutes(random.Next(5, 60));
					var risk = (RiskLevel)random.Next(1, 8 + 1);

					tracingKeys.Add(new TemporaryExposureKey(rnd, nowDate, duration, risk));
				}
			};

			return tracingKeys;
		}

		public static TemporaryExposureKeyExport GenerateTemporaryExposureKeyExport(int daysBack)
		{
			var rnd = new Random();

			var keys = GenerateTemporaryExposureKeys(daysBack);

			var keysByTime = keys.OrderBy(k => k.RollingStartIntervalNumber);

			var start = DateTimeOffset.FromUnixTimeSeconds(keysByTime.First().RollingStartIntervalNumber);
			var end = DateTimeOffset.FromUnixTimeSeconds(keysByTime.Last().RollingStartIntervalNumber);

			var export = new TemporaryExposureKeyExport
			{
				BatchNum = 1,
				BatchSize = 1,
				StartTimestamp = (ulong)keysByTime.First().RollingStartIntervalNumber,
				EndTimestamp = (ulong)keysByTime.Last().RollingStartIntervalNumber,
				Region = "default",
				Keys = { keys }
			};
			return export;
		}

		public static MemoryStream GetBin(this ZipArchive zip) =>
			zip.GetContents("export.bin", TemporaryExposureKeyExport.Header.Length);

		public static MemoryStream GetSignature(this ZipArchive zip) =>
			zip.GetContents("export.sig");

		public static MemoryStream GetContents(this ZipArchive zip, string entryName, int offset = 0)
		{
			using var entryStream = zip.GetEntry(entryName).Open();

			if (offset > 0)
			{
				Span<byte> skipper = stackalloc byte[offset];
				entryStream.Read(skipper);
			}

			var ms = new MemoryStream();
			entryStream.CopyTo(ms);
			ms.Position = 0;
			return ms;
		}
	}
}
