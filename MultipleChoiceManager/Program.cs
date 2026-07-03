using Microsoft.EntityFrameworkCore;
using MultipleChoiceManager.Data;
using MultipleChoiceManager.Services;
using MultipleChoiceManager.Services.Ai;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection("Gemini"));
builder.Services.AddScoped<IQuestionAiService, GeminiQuestionAiService>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseAzureSql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Dateispeicher: Azure Blob Storage (für lokale Dummy-Ablage stattdessen
// LocalFileStorageService registrieren).
builder.Services.AddSingleton<IFileStorageService, AzureBlobStorageService>();

var app = builder.Build();

// Migrationen anwenden und Demo-Daten einspielen, falls die Datenbank leer ist.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
    await DbSeeder.SeedAsync(dbContext);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

// Die eigentliche App ist unter /app erreichbar und startet mit den Lehrveranstaltungen.
// Da diese Route vor der Default-Route registriert ist, erzeugen auch alle Links auf
// Courses/Index die URL /app.
app.MapControllerRoute(
    name: "app",
    pattern: "app",
    defaults: new { controller = "Courses", action = "Index" })
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
