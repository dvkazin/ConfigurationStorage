using System.Reflection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Hosting.WindowsServices;
using ConfigurationStorage.AccessControl.Middleware;


#region Настройка

WebApplicationBuilder builder;

builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
	Args = args,
	ContentRootPath = AppContext.BaseDirectory,
	WebRootPath = WebApplication.Create().Configuration.GetValue<string>("UiDirectory")

});

Environment.CurrentDirectory = AppContext.BaseDirectory;

#endregion


#region Для запуска в качестве службы

if (WindowsServiceHelpers.IsWindowsService())
{
	builder.Host.UseWindowsService();
}

#endregion


#region Добавление сервисов в контейнер

builder.Services.AddCors();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(option => option.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml")));

#endregion


#region Настройка конвеера обработки запросов

var app = builder.Build();
app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto });
app.UseCors(builder => builder.AllowAnyOrigin());
//if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseSwagger(); app.UseSwaggerUI();
app.UseStaticFiles();
app.UseCustomAuthorization();
app.MapControllers();

#endregion


app.Run();