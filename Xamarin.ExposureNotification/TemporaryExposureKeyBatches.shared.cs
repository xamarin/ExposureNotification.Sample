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
		public const int MaxKeysPerFile = 18_000;

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

				var file = new Proto.File();
				file.Key.AddRange(batch.Select(k => new Proto.Key
				{
					KeyData = ByteString.CopyFrom(k.KeyData),
					RollingStartNumber = (uint)k.RollingStartLong,
					RollingPeriod = (uint)(k.RollingDuration.TotalMinutes / 10.0),
					TransmissionRiskLevel = (int)k.TransmissionRiskLevel,
				}));

				await AddBatchAsync(file);
			}
		}

		public Task AddBatchAsync(Proto.File file)
		{
			if (file == null || file.Key == null || file.Key.Count <= 0)
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
