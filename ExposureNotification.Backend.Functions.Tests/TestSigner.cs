using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using ExposureNotification.Backend.Database;
using ExposureNotification.Backend.Signing;

namespace ExposureNotification.Backend.Functions.Tests
{
	public class TestSigner : ISigner
	{
		// TODO: You need to sign this with the key and mechanism
		// that google/apple shared with you
		public Task<byte[]> GenerateSignatureAsync(byte[] contents, DbSignerInfo signerInfo)
		{
			var pfx = File.ReadAllBytes("../../../../sample-ecdsa-p256-cert.pfx");

			// Turn this into a certificate object
			var keyVaultCert = new X509Certificate2(pfx);

			// Get the private key to use for creating the signature
			var ecdsaPrivateKey = keyVaultCert.GetECDsaPrivateKey();

			// Create our signature based on the contents
			var signature = ecdsaPrivateKey.SignData(contents, HashAlgorithmName.SHA256);

			return Task.FromResult(signature);
		}
	}
}
