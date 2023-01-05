var builder = WebApplication.CreateBuilder(
    new WebApplicationOptions
    {
        WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "wwwroot")
    }
    );
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.Run();

