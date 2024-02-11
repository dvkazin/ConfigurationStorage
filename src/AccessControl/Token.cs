using System.Text;
using System.Text.Json;

namespace ConfigurationStorage.AccessControl
{
	public class Token
	{
		public Dictionary<string, string> Payload { get; set; } 
			= new Dictionary<string, string>();

		public byte[] Sign { get; private set; }

		public Token(int lifetime)
		{
			Payload.Add("Expire", DateTime.UtcNow.AddMinutes(lifetime).ToString());
			Sign = Crypto.Sign(Encoding.ASCII.GetBytes(JsonSerializer.Serialize(Payload)));
		}			
	}
}