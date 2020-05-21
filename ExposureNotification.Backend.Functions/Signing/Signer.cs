using System.Threading.Tasks;

namespace ExposureNotification.Backend.Signing
{
	public class Signer : ISigner
	{
		// TODO: You need to sign this with the key and mechanism
		// that google/apple shared with you
		public Task<byte[]> GenerateSignatureAsync(byte[] contents)
			=> Task.FromResult(new byte[0]);
	}
}
