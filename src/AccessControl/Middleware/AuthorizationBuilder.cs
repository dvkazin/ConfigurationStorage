namespace ConfigurationStorage.AccessControl.Middleware
{
	public static class AuthorizationBuilder
	{
		public static IApplicationBuilder UseCustomAuthorization(this IApplicationBuilder app) =>
			app.UseMiddleware<AuthorizationMiddleware>();
	}
}