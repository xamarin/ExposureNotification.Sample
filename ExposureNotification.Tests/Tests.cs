using System;
using Xunit;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Xamarin.ExposureNotifications;
using ExposureNotification.Backend;
using System.IO;

namespace ExposureNotification.Tests
{
	public class Tests
	{
		static string cacheRoot = Path.Combine(Path.GetTempPath(), "ExposureNotification.Tests");

		public Tests()
		{
			Storage = new ExposureNotificationStorage(
				builder => builder.UseInMemoryDatabase("Tests"),
				initialize => initialize.Database.EnsureCreated());
		}

		public ExposureNotificationStorage Storage { get; }

		[Fact]
		public async Task Add_Keys_Test()
		{
			var diagnosisUids = new[] { "adduid1", "adduid2", "adduid3" };

			await Storage.AddDiagnosisUidsAsync(diagnosisUids);

			foreach (var d in diagnosisUids)
				Assert.True(await Storage.CheckIfDiagnosisUidExistsAsync(d));
		}

		[Fact]
		public async Task Add_Duplicate_Keys_Test()
		{
			var diagnosisUids = new[] { "dupadduid1", "dupadduid2", "dupadduid3" };

			await Storage.AddDiagnosisUidsAsync(diagnosisUids);

			await Storage.AddDiagnosisUidsAsync(diagnosisUids);

			foreach (var d in diagnosisUids)
				Assert.True(await Storage.CheckIfDiagnosisUidExistsAsync(d));
		}

		[Fact]
		public async Task Remove_Keys_Test()
		{
			var diagnosisUids = new[] { "rmuid1", "rmuid2", "rmuid3" };

			await Storage.AddDiagnosisUidsAsync(diagnosisUids);

			foreach (var d in diagnosisUids)
				Assert.True(await Storage.CheckIfDiagnosisUidExistsAsync(d));

			await Storage.RemoveDiagnosisUidsAsync(diagnosisUids);

			foreach (var d in diagnosisUids)
				Assert.False(await Storage.CheckIfDiagnosisUidExistsAsync(d));
		}

		[Fact]
		public async Task Submit_Diagnosis_Test()
		{
			var keys = GenerateTemporaryExposureKeys(14);

			await Storage.AddDiagnosisUidsAsync(new[] { "posuid1" });

			await Storage.SubmitPositiveDiagnosisAsync(new ExposureNotificationStorage.SelfDiagnosisSubmissionRequest
			{
				DiagnosisUid = "posuid1",
				Keys = keys
			});

			var allKeys = new List<TemporaryExposureKey>();

			var skip = 0;
			var take = 10;

			while (true)
			{
				var r = await Storage.GetKeysAsync(0, skip, take);

				if (!r.Keys.Any())
					break;

				allKeys.AddRange(r.Keys);

				skip += take;
			}

			var keyToEnsureExists = keys.Skip(keys.Count / 2).First();

			Assert.Contains(allKeys, p => p.KeyData.SequenceEqual(keyToEnsureExists.KeyData));
		}

		[Fact]
		public async Task Submit_Diagnosis_Fails_Test()
		{
			var keys = GenerateTemporaryExposureKeys(14);

			await Assert.ThrowsAsync<InvalidOperationException>(async () =>
			{
				await Storage.SubmitPositiveDiagnosisAsync(new ExposureNotificationStorage.SelfDiagnosisSubmissionRequest
				{
					DiagnosisUid = "notaddeduid1",
					Keys = keys
				});
			});
		}

		[Fact]
		public async Task Page_Keys_Test()
		{
			var keys = GenerateTemporaryExposureKeys(1);

			var expectedCount = keys.Count();

			Storage.DeleteAllKeysAsync();

			await Storage.AddDiagnosisUidsAsync(new[] { "testkeys" });

			await Storage.SubmitPositiveDiagnosisAsync(
				new ExposureNotificationStorage.SelfDiagnosisSubmissionRequest
				{
					DiagnosisUid = "testkeys",
					Keys = keys
				});

			var actualCount = 0L;

			var skip = 0;
			var take = 10;
			ulong latestIndex = 0;

			while (true)
			{
				var keyBatch = await Storage.GetKeysAsync(
					0,
					skip,
					take);

				skip += take;

				var batchCount = keyBatch.Keys.Count();

				if (keyBatch.Latest > latestIndex)
					latestIndex = keyBatch.Latest;

				actualCount += batchCount;

				if (batchCount <= 0)
					break;
			}

			Assert.Equal(expectedCount, actualCount);
		}

		List<TemporaryExposureKey> GenerateTemporaryExposureKeys(int daysBack)
		{
			var tracingKeys = new List<TemporaryExposureKey>();

			var nowDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 0, 0, 0, 0, DateTimeKind.Utc);
			var random = new Random();

			for (var day = nowDate.AddDays(-1 * daysBack); day <= nowDate; day += TimeSpan.FromDays(1))
			{
				for (var seg = nowDate; seg < nowDate.AddDays(1); seg += TimeSpan.FromMinutes(15))
				{
					var rnd = new byte[64];
					random.NextBytes(rnd);

					tracingKeys.Add(new TemporaryExposureKey
					{
						KeyData = rnd,
						RollingStart = nowDate,
						RollingDuration = TimeSpan.FromMinutes(random.Next(5, 60)),
						TransmissionRiskLevel = (RiskLevel)random.Next(1, 8)
					});
				}
			};

			return tracingKeys;
		}

		[Fact]
		public async Task Batcher_Batches_Max_18000_Test()
		{
			// create 30K keys
			var keys = Enumerable.Range(1, 50_000).Select(id => new TemporaryExposureKey(BitConverter.GetBytes(id), DateTimeOffset.Now, TimeSpan.Zero, RiskLevel.Medium));

			using var batcher = new TemporaryExposureKeyBatches(cacheRoot);

			await batcher.AddBatchAsync(keys);

			Assert.True(batcher.HasFiles);
			Assert.Equal(3, batcher.Files.Count);
		}

		[Fact]
		public async Task Batcher_Batches_Max_18000_From_Single_Batch_Test()
		{
			// create 10K keys
			var keys = Enumerable.Range(1, 10_000).Select(id => new TemporaryExposureKey(BitConverter.GetBytes(id), DateTimeOffset.Now, TimeSpan.Zero, RiskLevel.Medium));

			using var batcher = new TemporaryExposureKeyBatches(cacheRoot);

			await batcher.AddBatchAsync(keys);

			Assert.True(batcher.HasFiles);
			Assert.Single(batcher.Files);
		}

		[Fact]
		public async Task Batcher_Batches_Max_18000_From_Smaller_Batches_Test()
		{
			// create 10K keys
			var keys = Enumerable.Range(1, 10_000).Select(id => new TemporaryExposureKey(BitConverter.GetBytes(id), DateTimeOffset.Now, TimeSpan.Zero, RiskLevel.Medium));

			using var batcher = new TemporaryExposureKeyBatches(cacheRoot);

			for (var i = 0; i < 6; i++)
			{
				await batcher.AddBatchAsync(keys);
			}

			Assert.True(batcher.HasFiles);
			Assert.Equal(6, batcher.Files.Count);
		}

		[Fact]
		public async Task Batcher_Batches_Min_0_Test()
		{
			// create 0 keys
			var keys = Enumerable.Range(1, 0).Select(id => new TemporaryExposureKey(BitConverter.GetBytes(id), DateTimeOffset.Now, TimeSpan.Zero, RiskLevel.Medium));

			using var batcher = new TemporaryExposureKeyBatches(cacheRoot);

			await batcher.AddBatchAsync(keys);

			Assert.False(batcher.HasFiles);
			Assert.Empty(batcher.Files);
		}

		[Fact]
		public void Empty_Batcher_Test()
		{
			using var batcher = new TemporaryExposureKeyBatches(cacheRoot);

			Assert.False(batcher.HasFiles);
			Assert.Empty(batcher.Files);
		}
	}
}
