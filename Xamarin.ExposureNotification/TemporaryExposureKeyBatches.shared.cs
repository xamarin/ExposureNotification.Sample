using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;
using Xamarin.Essentials;

namespace Xamarin.ExposureNotifications
{
	public class TemporaryExposureKeyBatches : ITemporaryExposureKeyBatches, IDisposable
	{
		public const int MaxKeysPerFile = 17_000;

		readonly string cacheRoot;

		public TemporaryExposureKeyBatches()
		{
			cacheRoot = FileSystem.CacheDirectory;
		}

		public TemporaryExposureKeyBatches(string cacheRoot)
		{
			this.cacheRoot = cacheRoot;
		}

		public List<string> Files { get; } = new List<string>();

		public bool HasFiles => Files.Count > 0;

		public async Task AddBatchAsync(IEnumerable<TemporaryExposureKey> keys)
		{
			if (keys?.Any() != true)
				return;

			// Batch up the keys and save into temporary files
			var sequence = keys;
			while (sequence.Any())
			{
				var batch = sequence.Take(MaxKeysPerFile);
				sequence = sequence.Skip(MaxKeysPerFile);

				var file = new TemporaryExposureKeyExport();
				file.Keys.AddRange(batch);

				await AddBatchAsync(file);
			}
		}

		public Task AddBatchAsync(TemporaryExposureKeyExport file)
		{
			if (file == null || file.Keys == null || file.Keys.Count <= 0)
				return Task.CompletedTask;

			if (!Directory.Exists(cacheRoot))
				Directory.CreateDirectory(cacheRoot);

			var batchFile = Path.Combine(cacheRoot, Guid.NewGuid().ToString());

			using var stream = File.Create(batchFile);
			using var coded = new CodedOutputStream(stream);
			file.WriteTo(coded);

			Files.Add(batchFile);

			return Task.CompletedTask;
		}

		public void Dispose()
		{
			foreach (var file in Files)
			{
				try
				{
					File.Delete(file);
				}
				catch
				{
					// no-op
				}
			}

			Files.Clear();
		}
	}
}
