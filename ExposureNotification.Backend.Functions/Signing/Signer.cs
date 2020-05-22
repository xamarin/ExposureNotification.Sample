using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using ExposureNotification.Backend.Proto;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;

namespace ExposureNotification.Backend.Signing
{
	public class Signer : ISigner
	{
		// TODO: You need to sign this with the key and mechanism
		// that google/apple shared with you
		public async Task<byte[]> GenerateSignatureAsync(byte[] contents)
		{
			// Get the configured secret ID
			var keyVaultSecretId = Environment.GetEnvironmentVariable("SigningKeyVaultSecretId", EnvironmentVariableTarget.Process);

			// Use a built in token provider which works in azure functions if the service/app identity is granted access to the key vault
			var azureServiceTokenProvider = new AzureServiceTokenProvider();

			// Create a key vault client instance with the token provider
			var keyVault = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));

			// Get the secret specified in the app config
			// This is actually am Elliptic Curve certificate (ECDSA) with a P-256 curve
			var keyVaultSecret = await keyVault.GetSecretAsync(keyVaultSecretId);

			// Turn this into a certificate object
			var keyVaultCert = new X509Certificate2(Convert.FromBase64String(keyVaultSecret.Value));

			// Get the private key to use for creating the signature
			var ecdsaPrivateKey = keyVaultCert.GetECDsaPrivateKey();

			// Create our signature based on the contents
			var signature = ecdsaPrivateKey.SignData(contents, HashAlgorithmName.SHA256);

			return signature;
		}
	}
}
