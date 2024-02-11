using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using ConfigurationStorage.AccessControl.Attribute;

namespace ConfigurationStorage.Controllers
{
	/// <summary>
	/// 
	/// </summary>
	[ApiController]
	[Route("api/profiles")]
	public class ProfilesController : ControllerBase
	{
		private readonly string linkName;
		private readonly string profilesDirectory;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="configuration"></param>
		public ProfilesController(IConfiguration configuration)
		{
			profilesDirectory = configuration.GetValue<string>("ProfilesDirectory") ?? string.Empty;

			if (OperatingSystem.IsWindows())
			{
				linkName = Path.Combine(profilesDirectory, "Default");
			}
			else
			{
				linkName = Path.Combine(profilesDirectory, "default");
			}
		}

		#region Профили

		/// <summary>
		/// Создаёт новый профиль
		/// </summary>
		/// <param name="profileName">Имя нового профиля</param>
		[HttpPost]
		[ProducesResponseType(StatusCodes.Status201Created)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status500InternalServerError)]
		public IActionResult AddProfile(string profileName)
		{
			try
			{
				var profilePath = Path.Combine(profilesDirectory, profileName);

				if (Directory.Exists(profilePath))
				{
					return StatusCode(StatusCodes.Status400BadRequest, new { Message = $"The {profileName} profile already exists." });
				}

				Directory.CreateDirectory(profilePath);

				return StatusCode(StatusCodes.Status201Created);
			}
			catch (Exception ex)
			{
				return StatusCode(StatusCodes.Status500InternalServerError, new { ex.Message });
			}
		}

		/// <summary>
		/// Возвращает список профилей
		/// </summary>
		/// <returns></returns>
		[WithoutAuthorization]
		[HttpGet]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status500InternalServerError)]
		public async Task<IActionResult> GetProfiles()
		{
			try
			{
				var profilesName = new List<object>();

				await Task.Run(() =>
				{
					var defaultProfile = Directory.Exists(linkName) ? Directory.ResolveLinkTarget(linkName, true)?.Name : string.Empty;
					foreach (var dir in Directory.GetDirectories(profilesDirectory))
					{
						if (dir.ToLower() != Path.Combine(profilesDirectory.ToLower(), "default"))
						{
							var profileName = Path.GetFileName(dir);
							profilesName.Add(new { Name = profileName, Default = profileName == defaultProfile });
						}
					}
				});

				return Ok(profilesName);
			}
			catch (Exception ex)
			{
				return StatusCode(StatusCodes.Status500InternalServerError, new { ex.Message });
			}
		}

		/// <summary>
		/// Возвращает список разделов профиля по умолчанию
		/// </summary>
		/// <returns></returns>
		[WithoutAuthorization]
		[HttpGet("default")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status500InternalServerError)]
		public IActionResult GetDefaultProfileSections()
		{
			try
			{
				var profileDirectory = Directory.ResolveLinkTarget(linkName, true)?.FullName ?? string.Empty;

				if (!Directory.Exists(linkName) ||
					Directory.ResolveLinkTarget(linkName, true) == null ||
					!Directory.Exists(profileDirectory))
				{
					return StatusCode(StatusCodes.Status404NotFound, new { Message = "The default profile is not set." });
				}

				return Ok(Sections(profileDirectory));
			}
			catch (Exception ex)
			{
				return StatusCode(StatusCodes.Status500InternalServerError, new { ex.Message });
			}
		}

		/// <summary>
		/// Возвращает раздел профиля
		/// </summary>
		/// <param name="profileName">Имя существующего профиля</param>
		/// <returns></returns>
		[WithoutAuthorization]
		[HttpGet("{profileName}")]
		public IActionResult GetSections(string profileName)
		{
			try
			{
				var profileDirectory = Path.Combine(profilesDirectory, profileName);

				if (!Directory.Exists(profileDirectory))
				{
					return StatusCode(StatusCodes.Status404NotFound, new { Message = $"The {profileName} profile does not exist." });
				}

				return Ok(Sections(profileDirectory));
			}
			catch (Exception ex)
			{
				return StatusCode(StatusCodes.Status500InternalServerError, new { ex.Message });
			}
		}

		/// <summary>
		/// Возвращает раздел профиля из профиля по умолчанию
		/// </summary>
		/// <param name="sectionName"></param>
		/// <returns></returns>
		[WithoutAuthorization]
		[HttpGet("default/{sectionName}")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status500InternalServerError)]
		public async Task<IActionResult> GetSectionDefaultPorile(string sectionName)
		{
			try
			{
				var profileDirectory = Directory.ResolveLinkTarget(linkName, true)?.FullName ?? string.Empty;

				if (!Directory.Exists(linkName) ||
					Directory.ResolveLinkTarget(linkName, true) == null ||
					!Directory.Exists(profileDirectory))
				{
					return StatusCode(StatusCodes.Status404NotFound, new { Message = "The default profile is not set." });
				}

				if (!System.IO.File.Exists(Path.Combine(profileDirectory, sectionName + ".json")))
				{
					return StatusCode(StatusCodes.Status404NotFound, new { Message = $"The {sectionName} section does not exist." });
				}

				var fullPath = Path.Combine(profileDirectory, sectionName + ".json");
				return Content(await System.IO.File.ReadAllTextAsync(fullPath), "application/json; charset=utf-8");
			}
			catch (Exception ex)
			{
				return StatusCode(StatusCodes.Status500InternalServerError, new { ex.Message });
			}
		}

		/// <summary>
		/// Устанавливает профиль по умолчанию
		/// </summary>
		/// <param name="profileName">Имя существующего профиля</param>
		/// <returns></returns>
		[HttpPut("default/{profileName}")]
		public IActionResult SetDefault(string profileName)
		{
			try
			{
				var profilePath = Path.Combine(profilesDirectory, profileName);

				if (!Directory.Exists(profilePath))
				{
					return StatusCode(StatusCodes.Status404NotFound, new { Message = $"The {profileName} profile does not exist." });
				}

				if (Directory.Exists(linkName))
				{
					Directory.Delete(linkName);
				}

				Directory.CreateSymbolicLink(linkName, profilePath);

				return StatusCode(StatusCodes.Status204NoContent);
			}
			catch (Exception ex)
			{
				return StatusCode(StatusCodes.Status500InternalServerError, new { ex.Message });
			}
		}

		/// <summary>
		/// Переименовывает профиль
		/// </summary>
		/// <param name="profileName">Имя существующего профиля</param>
		/// <param name="newProfileName">Новое имя профиля</param>
		/// <returns>200</returns>
		[HttpPut("{profileName}")]
		public IActionResult RenameProfile(string profileName, [FromBody] string newProfileName)
		{
			try
			{
				var profilePath = Path.Combine(profilesDirectory, profileName);

				if (!Directory.Exists(profilePath))
				{
					return StatusCode(StatusCodes.Status404NotFound, new { Message = $"The {profileName} profile does not exist." });
				}

				var newProfilePath = Path.Combine(profilesDirectory, newProfileName);

				if (Directory.Exists(newProfilePath))
				{
					return StatusCode(StatusCodes.Status400BadRequest, new { Message = $"The {newProfileName} profile already exists." });
				}

				Directory.Move(profilePath, newProfilePath);

				if (Directory.Exists(linkName) &&
					profilePath == Directory.ResolveLinkTarget(linkName, true)?.FullName)
				{
					Directory.Delete(linkName);
					Directory.CreateSymbolicLink(linkName, newProfilePath);
				}

				return StatusCode(StatusCodes.Status204NoContent);
			}
			catch (Exception ex)
			{
				return StatusCode(StatusCodes.Status500InternalServerError, new { ex.Message });
			}
		}

		/// <summary>
		/// Удаляет профиль. Профиль по умолчанию удалять нельзя
		/// </summary>
		/// <param name="profileName">Имя существующего профиля</param>
		/// <returns></returns>
		[HttpDelete("{profileName}")]
		public IActionResult DeleteProfile(string profileName)
		{
			try
			{
				var profilePath = Path.Combine(profilesDirectory, profileName);

				if (!Directory.Exists(profilePath))
				{
					return StatusCode(StatusCodes.Status404NotFound, new { Message = $"The {profileName} profile does not exist." });
				}

				if (Directory.Exists(linkName) &&
					profilePath == Directory.ResolveLinkTarget(linkName, true)?.FullName)
				{
					return StatusCode(StatusCodes.Status400BadRequest, new { Message = $"Can't delete default profile." });
				}

				Directory.Delete(profilePath, true);

				return StatusCode(StatusCodes.Status204NoContent);
			}
			catch (Exception ex)
			{
				return StatusCode(StatusCodes.Status500InternalServerError, new { ex.Message });
			}
		}

		/// <summary>
		/// Копирует профиль.
		/// Пример: /api/profiles/PostgreSQL?newProfileName=Production
		/// </summary>
		/// <param name="profileName">Имя существующего профиля</param>
		/// <param name="newProfileName">Имя нового профиля</param>
		/// <example>
		/// /api/profiles/PostgreSQL?newProfileName=Production
		/// </example>
		/// <returns></returns>
		[HttpPost("{profileName}")]
		public async Task<IActionResult> CopyProfile(string profileName, [FromQuery] string newProfileName)
		{
			try
			{
				var profilePath = Path.Combine(profilesDirectory, profileName);

				if (!Directory.Exists(profilePath))
				{
					return StatusCode(StatusCodes.Status404NotFound, new { Message = $"The {profileName} profile does not exist." });
				}

				var newProfilePath = Path.Combine(profilesDirectory, newProfileName);

				if (Directory.Exists(newProfilePath))
				{
					return StatusCode(StatusCodes.Status400BadRequest, new { Message = $"The {newProfileName} profile already exists." });
				}

				await Task.Run(() => CopyDirectory(profilePath, newProfilePath, true));

				return StatusCode(StatusCodes.Status201Created);
			}
			catch (Exception ex)
			{
				return StatusCode(StatusCodes.Status500InternalServerError, new { ex.Message });
			}
		}

		#endregion


		#region Раздел

		/// <summary>
		/// Переименовывает раздел
		/// </summary>
		/// <param name="profileName">Имя существующего профиля</param>
		/// <param name="sectionName">Имя раздела в этом профиле</param>
		/// <param name="newSectionName">Новое имя раздел</param>
		/// <returns></returns>
		[HttpPut("{profileName}/{sectionName}")]
		public IActionResult RenameSection(string profileName, string sectionName, [FromBody] string newSectionName)
		{
			try
			{
				if (!Directory.Exists(Path.Combine(profilesDirectory, profileName)))
				{
					return StatusCode(StatusCodes.Status404NotFound, new { Message = $"The {profileName} profile does not exist." });
				}

				var sectionFullPath = Path.Combine(profilesDirectory, profileName, sectionName + ".json");

				if (!System.IO.File.Exists(sectionFullPath))
				{
					return StatusCode(StatusCodes.Status404NotFound, new { Message = $"The {sectionName} section does not exist." });
				}

				var newSectionFullPath = Path.Combine(profilesDirectory, profileName, newSectionName + ".json");

				if (System.IO.File.Exists(newSectionFullPath))
				{
					return StatusCode(StatusCodes.Status400BadRequest, new { Message = $"The {newSectionName} section already exists." });
				}

				System.IO.File.Move(sectionFullPath, Path.Combine(profilesDirectory, profileName, newSectionFullPath));

				return StatusCode(StatusCodes.Status204NoContent);
			}
			catch (Exception ex)
			{
				return StatusCode(StatusCodes.Status500InternalServerError, new { ex.Message });
			}
		}

		/// <summary>
		/// Вносит изменения в раздел
		/// </summary>
		/// <param name="profileName">Имя существующего профиля</param>
		/// <param name="sectionName">Имя существующего раздела</param>
		/// <param name="sectionBody">Новое содержимое раздела</param>
		/// <returns></returns>
		[HttpPost("{profileName}/{sectionName}")]
		public async Task<IActionResult> ModifySection(string profileName, string sectionName, [FromBody] string sectionBody)
		{
			try
			{
				if (!Directory.Exists(Path.Combine(profilesDirectory, profileName)))
				{
					return StatusCode(StatusCodes.Status404NotFound, new { Message = $"The {profileName} profile does not exist." });
				}

				var sectionFullPath = Path.Combine(profilesDirectory, profileName, sectionName + ".json");

				if (!System.IO.File.Exists(sectionFullPath))
				{
					return StatusCode(StatusCodes.Status404NotFound, new { Message = $"The {sectionName} section does not exist." });
				}

				await System.IO.File.WriteAllTextAsync(sectionFullPath, sectionBody);

				return StatusCode(StatusCodes.Status201Created);
			}
			catch (Exception ex)
			{
				return StatusCode(StatusCodes.Status500InternalServerError, new { ex.Message });
			}
		}

		/// <summary>
		/// Возвращает содержимое раздела профиля
		/// </summary>
		/// <param name="profileName">Имя существующего профиля</param>
		/// <param name="sectionName">Название раздела</param>
		/// <returns></returns>
		[WithoutAuthorization]
		[HttpGet("{profileName}/{sectionName}")]
		public async Task<IActionResult> GetProfile(string profileName, string sectionName)
		{
			try
			{
				if (!Directory.Exists(Path.Combine(profilesDirectory, profileName)))
				{
					return StatusCode(StatusCodes.Status404NotFound, new { Message = $"The {profileName} profile does not exist." });
				}

				if (!System.IO.File.Exists(Path.Combine(profilesDirectory, profileName, sectionName + ".json")))
				{
					return StatusCode(StatusCodes.Status404NotFound, new { Message = $"The {sectionName} section does not exist." });
				}

				var fullPath = Path.Combine(profilesDirectory, profileName, sectionName + ".json");
				return Content(await System.IO.File.ReadAllTextAsync(fullPath), "application/json; charset=utf-8");
			}
			catch (Exception ex)
			{
				return StatusCode(StatusCodes.Status500InternalServerError, new { ex.Message });
			}
		}

		/// <summary>
		/// Удаляет раздел
		/// </summary>
		/// <param name="profileName">Имя существующего профиля</param>
		/// <param name="sectionName">Имя существующего раздела</param>
		/// <returns></returns>
		[HttpDelete("{profileName}/{sectionName}")]
		public IActionResult DeleteSection(string profileName, string sectionName)
		{
			try
			{
				if (!Directory.Exists(Path.Combine(profilesDirectory, profileName)))
				{
					return StatusCode(StatusCodes.Status404NotFound, new { Message = $"The {profileName} profile does not exist." });
				}

				var sectionFullPath = Path.Combine(profilesDirectory, profileName, sectionName + ".json");

				if (!System.IO.File.Exists(sectionFullPath))
				{
					return StatusCode(StatusCodes.Status404NotFound, new { Message = $"The {sectionName} section does not exist." });
				}

				System.IO.File.Delete(sectionFullPath);

				return StatusCode(StatusCodes.Status204NoContent);
			}
			catch (Exception ex)
			{
				return StatusCode(StatusCodes.Status500InternalServerError, new { ex.Message });
			}
		}

		/// <summary>
		/// Копирует раздел.
		/// Пример: /api/profiles/PostgreSQL/dms/copy?newSectionName=dms_backup
		/// </summary>
		/// <example>
		/// /api/profiles/PostgreSQL/dms/copy?newSectionName=dms_backup
		/// </example>
		/// <param name="profileName">Имя существующего профиля</param>
		/// <param name="sectionName">Имя существующего раздела</param>
		/// <param name="newSectionName">Имя нового раздела</param>
		/// <returns></returns>
		[HttpPost("{profileName}/{sectionName}/copy")]
		public IActionResult CopySection(string profileName, string sectionName, [FromQuery] string newSectionName)
		{
			try
			{
				if (!Directory.Exists(Path.Combine(profilesDirectory, profileName)))
				{
					return StatusCode(StatusCodes.Status404NotFound, new { Message = $"The {profileName} profile does not exist." });
				}

				var sectionFullPath = Path.Combine(profilesDirectory, profileName, sectionName + ".json");

				if (!System.IO.File.Exists(sectionFullPath))
				{
					return StatusCode(StatusCodes.Status404NotFound, new { Message = $"The {sectionName} section does not exist." });
				}

				var newSectionFullPath = Path.Combine(profilesDirectory, profileName, newSectionName + ".json");

				if (System.IO.File.Exists(newSectionFullPath))
				{
					return StatusCode(StatusCodes.Status400BadRequest, new { Message = $"The {newSectionName} section already exists." });
				}

				System.IO.File.Copy(sectionFullPath, newSectionFullPath);

				return StatusCode(StatusCodes.Status201Created);
			}
			catch (Exception ex)
			{
				return StatusCode(StatusCodes.Status500InternalServerError, new { ex.Message });
			}
		}

		#endregion

		/// <summary>
		/// Возвращает версию
		/// </summary>
		/// <returns></returns>
		[WithoutAuthorization]
		[HttpGet("/version")]
		public ContentResult GetVersion() =>
			Content(Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);

		private IEnumerable<string> Sections(string profileDirectory)
		{
			var sections = new List<string>();

			foreach (var file in Directory.GetFiles(profileDirectory))
			{
				sections.Add(Path.GetFileNameWithoutExtension(file));
			}

			return sections;
		}

		private void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
		{			
			var dir = new DirectoryInfo(sourceDir);

			if (!dir.Exists)
			{
				throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");
			}

			var dirs = dir.GetDirectories();
			
			Directory.CreateDirectory(destinationDir);

			
			foreach (FileInfo file in dir.GetFiles())
			{
				var targetFilePath = Path.Combine(destinationDir, file.Name);
				file.CopyTo(targetFilePath);
			}

			if (recursive)
			{
				foreach (DirectoryInfo subDir in dirs)
				{
					var newDestinationDir = Path.Combine(destinationDir, subDir.Name);
					CopyDirectory(subDir.FullName, newDestinationDir, true);
				}
			}
		}
	}
}