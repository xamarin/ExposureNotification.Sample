using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml;
using Xamarin.ExposureNotifications;
using Xamarin.ExposureNotifications.Proto;

namespace ExposureNotification.Backend
{
	public class ExposureNotificationStorage
	{
		public ExposureNotificationStorage(
			Action<DbContextOptionsBuilder> buildDbContextOpetions = null,
			Action<DbContext> initializeDb = null)
		{
			var dbContextOptionsBuilder = new DbContextOptionsBuilder();
			buildDbContextOpetions?.Invoke(dbContextOptionsBuilder);
			dbContextOptions = dbContextOptionsBuilder.Options;

			using (var ctx = new ExposureNotificationContext(dbContextOptions))
				initializeDb?.Invoke(ctx);
		}

		readonly DbContextOptions dbContextOptions;

		public Task<List<TemporaryExposureKey>> GetAllKeysAsync()
        {
			using (var ctx = new ExposureNotificationContext(dbContextOptions))
			{
				return ctx.TemporaryExposureKeys
					.Select(k => k.ToKey())
					.ToListAsync();
			}
		}
			

		public void DeleteAllKeysAsync()
		{
			using (var ctx = new ExposureNotificationContext(dbContextOptions))
			{
				ctx.TemporaryExposureKeys.RemoveRange(ctx.TemporaryExposureKeys);
				ctx.SaveChanges();
			}
		}

		public async Task AddDiagnosisUidsAsync(IEnumerable<string> diagnosisUids)
		{
			using (var ctx = new ExposureNotificationContext(dbContextOptions))
			{
				foreach (var d in diagnosisUids)
				{
					if (!(await ctx.Diagnoses.AnyAsync(r => r.DiagnosisUid == d)))
						ctx.Diagnoses.Add(new DbDiagnosis(d));
				}

				await ctx.SaveChangesAsync();
			}
		}

		public async Task RemoveDiagnosisUidsAsync(IEnumerable<string> diagnosisUids)
		{
			using (var ctx = new ExposureNotificationContext(dbContextOptions))
			{
				var toRemove = new List<DbDiagnosis>();

				foreach (var d in diagnosisUids)
				{
					var existingUid = await ctx.Diagnoses.FindAsync(d);
					if (existingUid != null)
						toRemove.Add(existingUid);
				}

				ctx.Diagnoses.RemoveRange(toRemove);
				await ctx.SaveChangesAsync();
			}
		}

		public Task<bool> CheckIfDiagnosisUidExistsAsync(string diagnosisUid)
		{
			using (var ctx = new ExposureNotificationContext(dbContextOptions))
				return Task.FromResult(ctx.Diagnoses.Any(d => d.DiagnosisUid.Equals(diagnosisUid)));
		}

		public async Task SubmitPositiveDiagnosisAsync(SelfDiagnosisSubmissionRequest diagnosis)
		{
			using (var ctx = new ExposureNotificationContext(dbContextOptions))
			{
				// Ensure the database contains the diagnosis uid
				if (!ctx.Diagnoses.Any(d => d.DiagnosisUid == diagnosis.DiagnosisUid))
					throw new InvalidOperationException();

				var dbKeys = diagnosis.Keys.Select(k => DbTemporaryExposureKey.FromKey(k)).ToList();

				foreach (var dbk in dbKeys)
					ctx.TemporaryExposureKeys.Add(dbk);

				await ctx.SaveChangesAsync();
			}
		}

		public async Task<int> GetNextBatchAsync(int batchNumber, string region, Func<File, Task> processBatch)
		{
			region ??= DbTemporaryExposureKey.DefaultRegion;

			var keyCount = 0;

			using (var ctx = new ExposureNotificationContext(dbContextOptions))
			using (var transaction = ctx.Database.BeginTransaction())
			{
				var keys = await ctx.TemporaryExposureKeys
					.Where(k => k.Region == region)
					.OrderBy(k => k.TimestampMsSinceEpoch)
					.Take(TemporaryExposureKeyBatches.MaxKeysPerFile)
					.ToListAsync();

				var exposureKeys = keys.Select(k => k.ToProtoKey());

				keyCount = exposureKeys.Count();

				var f = new File
				{
					Header = new Header
					{
						BatchNum = batchNumber,
						BatchSize = keyCount,
						StartTimestamp = keys.First().TimestampMsSinceEpoch,
						EndTimestamp = keys.Last().TimestampMsSinceEpoch,
						Region = region
					}
				};
				f.Key.AddRange(exposureKeys);

				await processBatch(f);

				ctx.TemporaryExposureKeys.RemoveRange(keys);

				await ctx.SaveChangesAsync();

				await transaction.CommitAsync();
			}

			return keyCount;
		}

		public class SelfDiagnosisSubmissionRequest
		{
			[JsonProperty("diagnosisUid")]
			public string DiagnosisUid { get; set; }

			[JsonProperty("keys")]
			public IEnumerable<TemporaryExposureKey> Keys { get; set; }
		}
	}
}
