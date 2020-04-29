using System;
using Xunit;
using ExposureNotification.Backend;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Internal;
using System.Linq;

namespace ExposureNotification.Tests
{
	public class Tests
	{
		public Tests()
		{
			Storage = new ExposureNotificationStorage(new FakeTemporaryExposureKeyEncoder(),
				builder => builder.UseInMemoryDatabase("Tests"),
				initialize => initialize.Database.EnsureCreated());
		}

		public ExposureNotificationStorage Storage { get; }

		[Fact]
		public void DefaultEncoder_Encode_Decode()
		{
			var path = AppDomain.CurrentDomain.BaseDirectory;

			// Public key only, for the client side encryption
			var encryptCert = new X509Certificate2(Path.Combine(path, "..", "..", "..", "..", "Certificates", "sample.ExposureNotification.xamarin.com.cert"));

			// Public + Private key for decrypting on the server
			var decryptCert = new X509Certificate2(Path.Combine(path, "..", "..", "..", "..", "Certificates", "sample.ExposureNotification.xamarin.com.pfx"));

			// Client side encoder with public key
			var encoder = new DefaultTemporaryExposureKeyEncoder(encryptCert);

			// Server side encoder with private + public keys
			var decoder = new DefaultTemporaryExposureKeyEncoder(decryptCert);

			// Create some random data to test with
			var data = new byte[64];
			var random = new Random();
			random.NextBytes(data);

			// Encrypt (client side)
			var encodedData = encoder.Encode(data);

			// Decrypt (server side)
			var decodedData = decoder.Decode(encodedData);

			Assert.Equal(data, decodedData);
		}

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

			await Storage.SubmitPositiveDiagnosisAsync("posuid1", keys);

			var positiveKeys = await Storage.GetKeysAsync(DateTime.UtcNow.AddDays(-14));

			var keyToEnsureExists = keys.Skip(keys.Count / 2).First();

			Assert.Contains(positiveKeys, new Predicate<TemporaryExposureKey>(p => p.KeyData.SequenceEqual(keyToEnsureExists.KeyData)));
		}

		[Fact]
		public async Task Submit_Diagnosis_Fails_Test()
		{
			var keys = GenerateTemporaryExposureKeys(14);

			await Assert.ThrowsAsync<InvalidOperationException>(async () => {
				await Storage.SubmitPositiveDiagnosisAsync("notaddeduid1", keys);
			});
		}

		[Fact]
		public async Task No_Old_Keys_Returned_Test()
		{
			var keys = GenerateTemporaryExposureKeys(20);

			await Storage.AddDiagnosisUidsAsync(new[] { "oldkeysuid1" });

			await Storage.SubmitPositiveDiagnosisAsync("oldkeysuid1", keys);

			var positiveKeys = await Storage.GetKeysAsync(DateTime.MinValue);

			Assert.DoesNotContain(positiveKeys, new Predicate<TemporaryExposureKey>(pk => pk.Timestamp < DateTime.UtcNow.AddDays(-14)));
		}


		[Fact]
		public async Task No_Old_Keys_Returned2_Test()
		{
			var keys = GenerateTemporaryExposureKeys(20);

			await Storage.AddDiagnosisUidsAsync(new[] { "oldkeysuid2" });

			await Storage.SubmitPositiveDiagnosisAsync("oldkeysuid2", keys);

			var positiveKeys = await Storage.GetKeysAsync(null);

			var oldest = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 0, 0, 0, 0, DateTimeKind.Utc);

			Assert.DoesNotContain(positiveKeys, new Predicate<TemporaryExposureKey>(pk => pk.Timestamp < oldest));
		}

		[Fact]
		public async Task Only_Keys_Since_Returned_Test()
		{
			var keys = GenerateTemporaryExposureKeys(20);

			await Storage.AddDiagnosisUidsAsync(new[] { "onlykeyssinceuid2" });

			await Storage.SubmitPositiveDiagnosisAsync("onlykeyssinceuid2", keys);

			var since = DateTime.UtcNow.AddDays(-7);

			var positiveKeys = await Storage.GetKeysAsync(since);

			Assert.DoesNotContain(positiveKeys, new Predicate<TemporaryExposureKey>(pk => pk.Timestamp < since));
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
						Timestamp = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day),
						KeyData = rnd
					});
				}
			};

			return tracingKeys;
		}
	}
}
