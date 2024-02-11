using Microsoft.AspNetCore.Mvc;
using ConfigurationStorage.DTO;

namespace ConfigurationStorage.Controllers
{
	/// <summary>
	/// 
	/// </summary>
	[ApiController]
	[Route("api/password")]
	public class PasswordController : ControllerBase
	{
		private readonly IConfiguration configuration;
		public PasswordController(IConfiguration configuration) =>
			this.configuration = configuration;

        /// <summary>
        /// Установить пароль
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        /// <response code="204">Success</response>
		[HttpPost]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Set([FromBody] Password password)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(password.New))
				{
					return StatusCode(StatusCodes.Status404NotFound, StatusCode(StatusCodes.Status400BadRequest, new { Message = $"Password Not Set" }));
				}

				var securityDirectory = configuration["SecurityDirectory"] ?? string.Empty;
				var fileWithPassword = Path.Combine(securityDirectory, "password");

				if (System.IO.File.Exists(fileWithPassword) && string.IsNullOrWhiteSpace(password.Current))
				{
					return StatusCode(StatusCodes.Status404NotFound, StatusCode(StatusCodes.Status400BadRequest, new { Message = $"Current Password Not Set" }));
				}
				
				if (System.IO.File.Exists(fileWithPassword))
				{
                    if (!BCrypt.Net.BCrypt.Verify(password.Current, System.IO.File.ReadAllText(fileWithPassword)))
                    {
                        return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Incorrect password" });
                    }
                }
								
                var workFactor = password.Cost > 0 ? password.Cost : 11;

				var hash = BCrypt.Net.BCrypt.HashPassword(password.New, workFactor: workFactor);

				await System.IO.File.WriteAllTextAsync(fileWithPassword, hash);

                return StatusCode(StatusCodes.Status204NoContent);
            }
			catch (Exception ex)
			{
				return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
			}
		}
	}
}