using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;
using ConfigurationStorage.AccessControl.Attribute;

namespace ConfigurationStorage.AccessControl.Middleware
{
	/// <summary>
	/// 
	/// </summary>
	public class AuthorizationMiddleware
	{
		private readonly RequestDelegate next;

		public AuthorizationMiddleware(RequestDelegate next) =>
			(this.next) = (next);

		public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
		{
			try
			{
				var endpoint = context.Features.Get<IEndpointFeature>()?.Endpoint;

				if (endpoint == null)
				{
					SendResponse(context, string.Empty, (int)HttpStatusCode.NotFound);
					return;
				}

				if (endpoint.Metadata.GetMetadata<WithoutAuthorizationAttribute>() != null)
				{
					await next.Invoke(context);
				}
				else
				{
					#region Провера наличия токена и срока его дейсвия

					var tokenBase64 = GetToken(context);
					
					if(string.IsNullOrWhiteSpace(tokenBase64))
					{
						SendResponse(context, string.Empty, (int)HttpStatusCode.Unauthorized);
						return;
					}
					
					var token = JsonSerializer.Deserialize<Token>(Convert.FromBase64String(tokenBase64));
					
					if (!Crypto.Verify(Encoding.ASCII.GetBytes(JsonSerializer.Serialize(token.Payload)), token.Sign))
					{
						SendResponse(context, string.Empty, (int)HttpStatusCode.Unauthorized);
						return;
					}

					if (Convert.ToDateTime(token.Payload["Expire"]).AddMinutes(Convert.ToInt32(configuration["TokenLifetime"])) < DateTime.UtcNow)
					{
						SendResponse(context, string.Empty, (int)HttpStatusCode.Unauthorized);
						return;
					}

					#endregion

					await next.Invoke(context);
				}
			}
			catch (Exception ex)
			{
				SendResponse(context, $"Message: \"{ex.Message}\"", (int)HttpStatusCode.BadRequest);
			}
		}

		private void SendResponse(HttpContext context, string message, int statusCode, string contentType = "application/json")
		{
			context.Response.StatusCode = statusCode;
			context.Response.ContentType = contentType;
			context.Response.WriteAsync(message).Wait();
		}

		private string GetToken(HttpContext context)
		{
			return context
				.Request
				.Headers["Authorization"]
				.ToString()
				.Replace("Bearer ", string.Empty);
		}
	}
}