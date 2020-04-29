using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace ExposureNotification
{
	public class DefaultTemporaryExposureKeyEncoder : ITemporaryExposureKeyEncoding
	{
		public DefaultTemporaryExposureKeyEncoder(X509Certificate2 certificate)
			=> Certificate = certificate;

		public X509Certificate2 Certificate { get; }

		public byte[] Encode(byte[] data)
			=> Certificate.GetRSAPublicKey().Encrypt(data, RSAEncryptionPadding.Pkcs1);

		public byte[] Decode(byte[] encryptedData)
			=> Certificate.GetRSAPrivateKey().Decrypt(encryptedData, RSAEncryptionPadding.Pkcs1);
	}
}
