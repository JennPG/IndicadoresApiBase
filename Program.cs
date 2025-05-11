using IndicadoresApi.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<ControlConexion>();
builder.Services.AddSingleton<TokenService>();

// Configuración de CORS mejorada para manejar múltiples puertos y redirecciones
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins",
        builder => builder
            // Permitir tanto el origen original como el redirigido
            .WithOrigins(
                "https://localhost:7230", // Origen de tu cliente Blazor
                "http://localhost:7230",  // Versión HTTP por si acaso
                "https://localhost:7237", // Puerto al que se redirige
                "http://localhost:7237",  // Versión HTTP del puerto redirigido
                "http://localhost:3000",  // Puerto original
                "https://localhost:3000"  // Versión HTTPS del puerto original
            )
            .SetIsOriginAllowedToAllowWildcardSubdomains() // Permitir subdominios
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()); // Importante para cookies/auth
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None; // Para CORS
    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest; // Flexible para desarrollo
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Api Genérica C#",
        Version = "v1",
        Description = "API de prueba con ASP.NET Core y Swagger",
        Contact = new OpenApiContact
        {
            Name = "Soporte API",
            Email = "soporte@miapi.com",
            Url = new Uri("https://miapi.com/contacto")
        }
    });
});

var app = builder.Build();

// Configuración para desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Api Genérica C#");
        c.RoutePrefix = "swagger";
    });

    // Importante: En desarrollo, podemos desactivar la redirección HTTPS para evitar problemas con CORS
    // COMENTAR ESTA LÍNEA si sigue habiendo problemas de redirección
    // app.UseHttpsRedirection();
}
else
{
    // En producción mantenemos la redirección HTTPS
    app.UseHttpsRedirection();
    app.UseHsts();
}

// IMPORTANTE: CORS debe estar ANTES de cualquier otro middleware que pudiera generar redirecciones
app.UseCors("AllowSpecificOrigins");

// Configuración del resto de middleware
app.UseSession();
app.UseRouting();
app.UseAuthorization();

app.MapControllers();

// Middleware especial para depurar CORS en desarrollo
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        // Log de información sobre la solicitud para depuración
        Console.WriteLine($"Solicitud recibida: {context.Request.Method} {context.Request.Path}");
        Console.WriteLine($"Origen: {context.Request.Headers["Origin"]}");

        // Continuar con la ejecución
        await next();

        // Log de información sobre la respuesta
        Console.WriteLine($"Código de estado: {context.Response.StatusCode}");

        // Para solicitudes CORS OPTIONS, asegurarse de que los encabezados estén presentes
        if (context.Request.Method == "OPTIONS")
        {
            if (!context.Response.Headers.ContainsKey("Access-Control-Allow-Origin"))
            {
                Console.WriteLine("⚠️ Encabezado Access-Control-Allow-Origin no encontrado en la respuesta");

                // Agregar encabezados CORS manualmente como último recurso
                context.Response.Headers.Append("Access-Control-Allow-Origin", context.Request.Headers["Origin"].ToString());
                context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                context.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type, Authorization");
                context.Response.Headers.Append("Access-Control-Allow-Credentials", "true");
                context.Response.StatusCode = 200;
            }
        }
    });
}

app.Run();

