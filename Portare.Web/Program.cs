using Portare.Web.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddAppServices();
var app = builder.Build();

app.UseAppServices();

app.Run();