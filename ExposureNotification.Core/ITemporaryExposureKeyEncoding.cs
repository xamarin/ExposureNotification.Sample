namespace ExposureNotification
{
	public interface ITemporaryExposureKeyEncoding
	{
		byte[] Encode(byte[] keyData);
		byte[] Decode(byte[] encodedKeyData);
	}
}
