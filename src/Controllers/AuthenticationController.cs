using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ConfigurationStorage.AccessControl;
using ConfigurationStorage.AccessControl.Attribute;

namespace ConfigurationStorage.Controllers
{
	[ApiController]
	[Route("api/authentication")]
	public class AuthenticationController : ControllerBase
	{
		private readonly IConfiguration configuration;
		public AuthenticationController(IConfiguration configuration) =>
			this.configuration = configuration;

        /// <summary>
        /// Аутентификация.
        /// После перезапуска сервиса, все tokens становятся невалидными.
		/// Время жизни token задаётся в параметре TokenLifetime
        /// </summary>
        /// <param name="password">Пароль</param>
        /// <returns></returns>
        [WithoutAuthorization]
		[HttpPost]
		public IActionResult Login([FromBody] string password)
		{
			try
			{
				var securityDirectory = configuration["SecurityDirectory"] ?? string.Empty;
				var fileWithPassword = Path.Combine(securityDirectory, "password");

				if (!System.IO.File.Exists(fileWithPassword))
				{
					return StatusCode(StatusCodes.Status404NotFound, StatusCode(StatusCodes.Status400BadRequest, new { Message = $"Password Not Set" }));
				}

				if (!BCrypt.Net.BCrypt.Verify(password, System.IO.File.ReadAllText(fileWithPassword)))
				{
					return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Incorrect password" });
				}

				var lifetime = Convert.ToInt32(configuration["TokenLifetime"]);
				if (lifetime < 1)
				{
					return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "TokenLifetime Parameter Not Set" });
				}

				var token = new Token(lifetime);

				return Ok(new { AccessToken = Convert.ToBase64String(Encoding.ASCII.GetBytes(JsonSerializer.Serialize(token))) });
			}
			catch (Exception ex)
			{
				return StatusCode(StatusCodes.Status400BadRequest, ex.Message);
			}
		}

		//TODO: Написать консольное приложение, для генерации hash
		/// <summary>
		/// Генерация хеша пароля
		/// </summary>
		/// <param name="value">Пароль</param>
		/// <param name="cost">Сложность пароля. С увеличением значения, увеличивается время на генерацию хеша. По умолчанию 11</param>
		/// <returns></returns>
		//[WithoutAuthorization]
		//[HttpGet]
		//[Route("hash/{value}")]
		//public IActionResult GenHash(string value, int cost)
		//{
		//	try
		//	{
		//		var workFactor = cost > 0 ? cost : 11;
		//		return Ok(new
		//		{
		//			Value = value,
		//			Hash = BCrypt.Net.BCrypt.HashPassword(value, workFactor: workFactor),
		//			WorkFactor = workFactor
		//		});
		//	}
		//	catch (Exception ex)
		//	{
		//		return StatusCode(StatusCodes.Status400BadRequest, ex.Message);
		//	}
		//}
	}
}