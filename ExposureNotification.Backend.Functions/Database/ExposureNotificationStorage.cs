using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExposureNotification.Backend.DeviceVerification;
using ExposureNotification.Backend.Functions;
using ExposureNotification.Backend.Network;
using ExposureNotification.Backend.Proto;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Options;

namespace ExposureNotification.Backend.Database
{
	public class ExposureNotificationStorage : IAsyncDisposable
	{
		// One person/diagnosis should never need to submit many many keys
		// and we can detect if they try to and assume it's malicious and prevent it
		// this is the threshold for how many keys we accept from a single diagnosis uid
		const int maxKeysPerDiagnosisUid = 30;

		ExposureNotificationContext context;
		Settings settings;

		public ExposureNotificationStorage(ExposureNotificationContext context, IOptions<Settings> settings)
		{
			this.context = context;
			this.settings = settings.Value;
		}

		public ValueTask DisposeAsync() =>
			context.DisposeAsync();

		public Task<List<TemporaryExposureKey>> GetAllKeysAsync() =>
			context.TemporaryExposureKeys
				.Select(k => k.ToKey())
				.ToListAsync();

		public void DeleteAllKeysAsync()
		{
			context.TemporaryExposureKeys.RemoveRange(context.TemporaryExposureKeys);
			context.SaveChanges();
		}

		public async Task AddDiagnosisUidsAsync(IEnumerable<string> diagnosisUids)
		{
			foreach (var d in diagnosisUids)
			{
				if (await context.Diagnoses.AllAsync(r => r.DiagnosisUid != d))
					context.Diagnoses.Add(new DbDiagnosis(d));
			}

			await context.SaveChangesAsync();
		}

		public async Task RemoveDiagnosisUidsAsync(IEnumerable<string> diagnosisUids)
		{
			var toRemove = new List<DbDiagnosis>();

			foreach (var d in diagnosisUids)
			{
				var existingUid = await context.Diagnoses.FindAsync(d);
				if (existingUid != null)
					toRemove.Add(existingUid);
			}

			context.Diagnoses.RemoveRange(toRemove);
			await context.SaveChangesAsync();
		}

		public Task<bool> CheckIfDiagnosisUidExistsAsync(string diagnosisUid) =>
			Task.FromResult(context.Diagnoses.Any(d => d.DiagnosisUid.Equals(diagnosisUid)));

		public async Task SubmitPositiveDiagnosisAsync(SelfDiagnosisSubmission diagnosis)
		{
			using var transaction = context.Database.BeginTransaction();

			// Ensure the database contains the diagnosis uid
			var dbDiag = await context.Diagnoses.FirstOrDefaultAsync(d => d.DiagnosisUid == diagnosis.VerificationPayload);

			// Check that the diagnosis uid exists and that there aren't too many keys associated
			// already, otherwise it might be someone submitting fake data with a legitimate key
			if (dbDiag == null || dbDiag.KeyCount > maxKeysPerDiagnosisUid)
				throw new InvalidOperationException();

			// Duplicate the key for each region so it gets included in the batch files for that region
			foreach (var region in settings.SupportedRegions)
			{
				var dbKeys = diagnosis.Keys.Select(k => DbTemporaryExposureKey.FromKey(k, region)).ToList();

				// Add the new keys to the db
				foreach (var dbk in dbKeys)
				{
					// Only add key if it doesn't exist already
					if (!await context.TemporaryExposureKeys.AnyAsync(k => k.Base64KeyData == dbk.Base64KeyData && k.Region == region))
						context.TemporaryExposureKeys.Add(dbk);
				}
			}

			// Increment key count
			dbDiag.KeyCount += diagnosis.Keys.Count();

			await context.SaveChangesAsync();

			await transaction.CommitAsync();
		}

		public async Task CreateBatchFilesAsync(string region, Func<TemporaryExposureKeyExport, Task> processExport)
		{
			using var transaction = context.Database.BeginTransaction();

			var cutoffMsEpoch = DateTimeOffset.UtcNow.AddDays(-14).ToUnixTimeMilliseconds();

			var keys = context.TemporaryExposureKeys
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
			if (settings.DeleteKeysFromDbAfterBatching)
			{
				context.TemporaryExposureKeys.RemoveRange(keys);
			}
			else
			{
				foreach (var k in keys)
					k.Processed = true;
			}

			await context.SaveChangesAsync();

			await transaction.CommitAsync();
		}

		// TODO: load this from a DB or config

		public Task<List<DbSignerInfo>> GetAllSignerInfosAsync()
			=> Task.FromResult(new List<DbSignerInfo> {
				new DbSignerInfo
				{
					AndroidPackage = "com.xamarin.exposurenotification.sampleapp",
					AppBundleId = "com.xamarin.exposurenotification.sampleapp",
					VerificationKeyId = "ExampleServer_k1",
					VerificationKeyVersion = "1",
					SigningKeyBase64String = settings.SigningKeyBase64String
				}
			});

		public DbAuthorizedApp GetAuthorizedApp(DbAuthorizedApp.DevicePlatform platform) =>
			platform switch
			{
				DbAuthorizedApp.DevicePlatform.Android => new DbAuthorizedApp
				{
					PackageName = "com.xamarin.exposurenotification.sampleapp",
					Platform = "android",
				},
				DbAuthorizedApp.DevicePlatform.iOS => new DbAuthorizedApp
				{
					PackageName = "com.xamarin.exposurenotification.sampleapp",
					Platform = "ios",
					DeviceCheckKeyId = "YOURKEYID",
					DeviceCheckTeamId = "YOURTEAMID",
					DeviceCheckPrivateKey = "CONTENTS-OF-P8-FILE-WITH-NO-LINE-BREAKS"
				},
				_ => throw new ArgumentOutOfRangeException(nameof(platform))
			};

		// helpers

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
