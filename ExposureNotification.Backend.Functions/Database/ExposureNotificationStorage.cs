using ExposureNotification.Backend.Functions;
using ExposureNotification.Backend.Network;
using ExposureNotification.Backend.Proto;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExposureNotification.Backend.Database
{
	public class ExposureNotificationStorage
	{
		// One person/diagnosis should never need to submit many many keys
		// and we can detect if they try to and assume it's malicious and prevent it
		// this is the threshold for how many keys we accept from a single diagnosis uid
		const int maxKeysPerDiagnosisUid = 30;

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

		public Task<List<DbSignerInfo>> GetAllSignerInfosAsync()
		{
			// TODO: load this from a DB or config
			var si = new List<DbSignerInfo>
			{
				new DbSignerInfo
				{
					AndroidPackage = "com.xamarin.exposurenotificationsample",
					AppBundleId = "com.xamarin.exposurenotificationsample",
					VerificationKeyId = "ExampleServer_k1",
					VerificationKeyVersion = "1",
				}
			};

			return Task.FromResult(si);
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

		public async Task SubmitPositiveDiagnosisAsync(SelfDiagnosisSubmission diagnosis)
		{
			using (var ctx = new ExposureNotificationContext(dbContextOptions))
			using (var transaction = ctx.Database.BeginTransaction())
			{
				// Ensure the database contains the diagnosis uid
				var dbDiag = await ctx.Diagnoses.FirstOrDefaultAsync(d => d.DiagnosisUid == diagnosis.VerificationPayload);

				// Check that the diagnosis uid exists and that there aren't too many keys associated
				// already, otherwise it might be someone submitting fake data with a legitimate key
				if (dbDiag == null || dbDiag.KeyCount > maxKeysPerDiagnosisUid)
					throw new InvalidOperationException();

				var dbKeys = diagnosis.Keys.Select(k => DbTemporaryExposureKey.FromKey(k)).ToList();

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

		public async Task CreateBatchFilesAsync(string region, Func<TemporaryExposureKeyExport, Task> processExport)
		{
			region ??= DbTemporaryExposureKey.DefaultRegion;

			using (var ctx = new ExposureNotificationContext(dbContextOptions))
			using (var transaction = ctx.Database.BeginTransaction())
			{
				var cutoffMsEpoch = DateTimeOffset.UtcNow.AddDays(-14).ToUnixTimeMilliseconds();

				var keys = ctx.TemporaryExposureKeys
					.Where(k => k.Region == region
								&& !k.Processed
								&& k.TimestampMsSinceEpoch >= cutoffMsEpoch);

				// How many keys do we need to put in batchfiles
				var totalCount = await keys.CountAsync();

				// How many files do we need to fit all the keys
				var batchFileCount = (int)Math.Ceiling((double)totalCount / (double)TemporaryExposureKeyExport.MaxKeysPerFile);

				for (var i = 0; i < batchFileCount; i++)
				{
					var batchFileKeys = keys
						.Skip(i * TemporaryExposureKeyExport.MaxKeysPerFile)
						.Take(TemporaryExposureKeyExport.MaxKeysPerFile)
						.ToArray();

					var export = CreateUnsignedExport(region, i + 1, batchFileCount, batchFileKeys);

					await processExport(export);
				}

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
		}

		public static TemporaryExposureKeyExport CreateUnsignedExport(string region, int batchNumber, int batchCount, IEnumerable<DbTemporaryExposureKey> keys)
		{
			var keysByTime = keys.OrderBy(k => k.TimestampMsSinceEpoch);

			return new TemporaryExposureKeyExport
			{
				BatchNum = batchNumber,
				BatchSize = batchCount,
				StartTimestamp = (ulong)(keysByTime.First().TimestampMsSinceEpoch / 1000),
				EndTimestamp = (ulong)(keysByTime.Last().TimestampMsSinceEpoch / 1000),
				Region = region,
				Keys = { keys.OrderBy(k => k.Base64KeyData).Select(k => k.ToKey()) },
			};
		}
	}
}
