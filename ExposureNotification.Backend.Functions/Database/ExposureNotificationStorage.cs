using ExposureNotification.Backend.Functions;
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

namespace ExposureNotification.Backend
{
	public class ExposureNotificationStorage
	{
		// One person/diagnosis should never need to submit many many keys
		// and we can detect if they try to and assume it's malicious and prevent it
		// this is the threshold for how many keys we accept from a single diagnosis uid
		const int maxKeysPerDiagnosisFile = 30;

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
			using (var transaction = ctx.Database.BeginTransaction())
			{
				// Ensure the database contains the diagnosis uid
				var dbDiag = await ctx.Diagnoses.FirstOrDefaultAsync(d => d.DiagnosisUid == diagnosis.DiagnosisUid);
				
				// Check that the diagnosis uid exists and that there aren't too many keys associated
				// already, otherwise it might be someone submitting fake data with a legitimate key
				if (dbDiag == null || dbDiag.KeyCount > maxKeysPerDiagnosisFile)
					throw new InvalidOperationException();

				var dbKeys = diagnosis.Keys.Select(k => DbTemporaryExposureKey.FromKey(k, diagnosis.TestDate)).ToList();

				// Add the new keys to the db
				foreach (var dbk in dbKeys)
				{
					// Only add key if it doesn't exist already
					if (!await ctx.TemporaryExposureKeys.AnyAsync(k => k.Base64KeyData == dbk.Base64KeyData))
						ctx.TemporaryExposureKeys.Add(dbk);
				}

				// Increment key count
				dbDiag.KeyCount += diagnosis.Keys.Count();

				await ctx.SaveChangesAsync();

				await transaction.CommitAsync();
			}
		}

		public async Task<int> GetNextBatchAsync(int batchNumber, string region, Func<TemporaryExposureKeyBatch, Task> processBatch)
		{
			region ??= DbTemporaryExposureKey.DefaultRegion;

			var keyCount = 0;

			using (var ctx = new ExposureNotificationContext(dbContextOptions))
			using (var transaction = ctx.Database.BeginTransaction())
			{
				var keys = await ctx.TemporaryExposureKeys
					.Where(k => k.Region == region && !k.Processed)
					.OrderBy(k => k.TimestampMsSinceEpoch)
					.Take(TemporaryExposureKeyBatches.MaxKeysPerFile)
					.ToListAsync();

				var exposureKeys = keys.Select(k => k.ToProtoKey());

				keyCount = exposureKeys.Count();

				var f = new TemporaryExposureKeyBatch
				{
					Header = new TemporaryExposureKeyBatchHeader
					{
						BatchNum = batchNumber,
						BatchSize = keyCount,
						StartTimestamp = keys.First().TestDateMsSinceEpoch,
						EndTimestamp = keys.Last().TestDateMsSinceEpoch,
						Region = region
					}
				};
				f.Key.AddRange(exposureKeys);

				await processBatch(f);

				// Decide to delete keys or just mark them as processed
				// Marking as processed is better 
				if (Startup.DeleteKeysFromDbAfterBatching)
				{
					ctx.TemporaryExposureKeys.RemoveRange(keys);
				}
				else
				{
					foreach (var k in keys)
						k.Processed = true;
				}

				await ctx.SaveChangesAsync();

				await transaction.CommitAsync();
			}

			return keyCount;
		}

		public class SelfDiagnosisSubmissionRequest
		{
			[JsonProperty("diagnosisUid")]
			public string DiagnosisUid { get; set; }

			[JsonProperty("testDate")]
			public long TestDate { get; set; }

			[JsonProperty("keys")]
			public IEnumerable<TemporaryExposureKey> Keys { get; set; }
		}
	}
}
