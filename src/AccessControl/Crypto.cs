using System.Security.Cryptography;

namespace ConfigurationStorage.AccessControl
{
	public class Crypto
	{
		private static byte[] key = Guid.NewGuid().ToByteArray();

		public static byte[] Sign(byte[] value)
		{
			using var hmac = new HMACSHA256(key);
			return hmac.ComputeHash(value);
		}

		public static bool Verify(byte[] value, byte[] sign) =>
			Sign(value).SequenceEqual(sign);
	}
}