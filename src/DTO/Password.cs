namespace ConfigurationStorage.DTO
{
	/// <summary>
	/// Используется для передачи нового пароля
	/// </summary>
	public class Password
	{
		/// <summary>
		/// Новый пароль
		/// </summary>
		public string New { get; set; } = string.Empty;
		/// <summary>
		/// Текущий пароль
		/// </summary>
		public string Current { get; set; } = string.Empty;
		/// <summary>
		/// Сложность алгоритма.
		/// </summary>
		public int Cost { get; set; }
    }
}
