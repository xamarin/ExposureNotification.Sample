using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using ExposureNotification.Backend.Database;
using ExposureNotification.Backend.Functions;
using ExposureNotification.Backend.Proto;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;

namespace ExposureNotification.Backend.Signing
{
	public class Signer : ISigner
	{
		// TODO: You need to sign this with the key and mechanism
		// that google/apple shared with you
		public Task<byte[]> GenerateSignatureAsync(byte[] contents, DbSignerInfo signerInfo)
		{
			// This is actually am Elliptic Curve certificate (ECDSA) with a P-256 curve
			// It's been encoded to a base64 string
			// Turn this into a certificate object
			var keyVaultCert = new X509Certificate2(Convert.FromBase64String(signerInfo.SigningKeyBase64String));

			// Get the private key to use for creating the signature
			var ecdsaPrivateKey = keyVaultCert.GetECDsaPrivateKey();

			// Create our signature based on the contents
			var signature = ecdsaPrivateKey.SignData(contents, HashAlgorithmName.SHA256);

			return Task.FromResult(signature);
		}
	}
}
