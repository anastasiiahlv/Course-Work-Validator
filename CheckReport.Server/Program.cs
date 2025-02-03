var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("https://localhost:52377")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
            //policy.SetIsOriginAllowed(_ => true).AllowAnyMethod().AllowAnyHeader();
        });
});

var app = builder.Build();

app.UseDefaultFiles();
app.MapStaticAssets();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Use(async (context, next) =>
{
    context.Response.Headers["Content-Type"] = "text/html; charset=UTF-8";
    await next();
});

app.Run();
