using Alertas.Data;
using Alertas.Services;
using Alertas.Services.CargaMasiva;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Alertas.Services.Notificaciones;
using Alertas.Services.Notificaciones.Config;
using Alertas.Services.Storage.Interfaces;
using Alertas.Services.Storage.Local;
using Alertas.Services.Storage.Interfaces;
using Alertas.Services.Storage.Local;
using Alertas.Services.Storage.Models;
using Alertas.Services.Storage.R2;
using Alertas.Services.Storage;
using Alertas.Services.Storage.Cleanup;
using System.IO;
using Microsoft.AspNetCore.DataProtection;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHostedService<StorageCleanupBackgroundService>();

builder.Services.AddScoped<IFileStorageService,
    LocalFileStorageService>();

builder.Services.Configure<StorageSettings>(
    builder.Configuration.GetSection("StorageSettings"));

// DbContext PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<FileUploadValidator>();

// MVC
builder.Services.AddControllersWithViews();

// HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Session
builder.Services.AddDistributedMemoryCache();

builder.Services.AddScoped<IExcelObligacionesReader, ExcelObligacionesReader>();

builder.Services.AddScoped<IValidadorCargaObligacionesService, ValidadorCargaObligacionesService>();

builder.Services.AddScoped<IConfirmadorCargaObligacionesService, ConfirmadorCargaObligacionesService>();

builder.Services.AddScoped<IExcelErroresCargaService, ExcelErroresCargaService>();

/*builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"))
    .SetApplicationName("Alertas");*/

builder.Services.AddHttpClient();

builder.Services.Configure<ResendSettings>(
    builder.Configuration.GetSection("Resend"));

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "Alertas.Session";
});

// Authentication - Cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Index";
        options.LogoutPath = "/Login/Logout";
        options.AccessDeniedPath = "/Login/AccessDenied";

        options.Cookie.Name = "Alertas.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;

        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

// Authorization
builder.Services.AddAuthorization();

builder.Services.AddScoped<SeguridadService>();

builder.Services.AddScoped<IPlantillaObligacionesService, PlantillaObligacionesService>();

builder.Services.AddScoped<ICalendarioHabilesService, CalendarioHabilesService>();

builder.Services.AddScoped<INotificacionesAlertasService, NotificacionesAlertasService>();

builder.Services.Configure<SmtpSettings>(
    builder.Configuration.GetSection("Smtp"));

builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddScoped<IPlantillaCorreoAlertasService, PlantillaCorreoAlertasService>();

var storageProvider = builder.Configuration["StorageSettings:Provider"];

if (storageProvider == "R2")
{
    builder.Services.AddScoped<IFileStorageService, R2StorageService>();
}
else
{
    builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
}


var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

app.Run();