using System.Threading.Tasks;

namespace ExposureNotification.Backend.Signing
{
	public interface ISigner
	{
		Task<byte[]> GenerateSignatureAsync(byte[] contents);
	}
}
