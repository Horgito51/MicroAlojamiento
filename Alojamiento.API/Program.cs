using System.Text;
using Alojamiento.Business.Interfaces.Alojamiento;
using System.Security.Claims;
using System.Text.Json;
using Alojamiento.Business.Interfaces.Booking;
using Alojamiento.Business.Interfaces.Valoraciones;
using Alojamiento.Business.Services.Alojamiento;
using Alojamiento.Business.Services.Booking;
using Alojamiento.Business.Services.Valoraciones;
using Alojamiento.API.GrpcServices;
using Alojamiento.API.Services;
using Alojamiento.DataAccess.Context;
using Alojamiento.DataAccess.Repositories.Alojamiento;
using Alojamiento.DataAccess.Repositories.Interfaces.Alojamiento;
using Alojamiento.DataAccess.Repositories.Interfaces.Valoraciones;
using Alojamiento.DataAccess.Repositories.Valoraciones;
using Alojamiento.DataManagement.Alojamiento.Interfaces;
using Alojamiento.DataManagement.Alojamiento.Services;
using Alojamiento.DataManagement.UnitOfWork;
using Alojamiento.DataManagement.Valoraciones.Interfaces;
using Alojamiento.DataManagement.Valoraciones.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using Reservas.Contracts.Grpc.V1;

const string LocalCorsPolicy = "LocalCorsPolicy";

string[] backOfficeRoles =
[
    "ADMINISTRADOR",
    "ADMIN",
    "RECEPCIONISTA",
    "OPERATIVO",
    "DESK_SERVICE"
];

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);
ConfigureHttpEndpoint(builder);
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Services.AddHealthChecks();
var disableAuthorizationForTesting = builder.Configuration.GetValue<bool>("Security:DisableAuthorizationForTesting");
var connectionString = ResolveDefaultConnectionString(builder.Configuration, builder.Environment);

builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Microservicio Alojamiento API", Version = "v1" });
    options.OperationFilter<AccommodationDetailSwaggerCleanupFilter>();
    options.SchemaFilter<AccommodationDetailSchemaFilter>();
    if (!disableAuthorizationForTesting)
    {
        AddBearerSecurity(options);
        options.OperationFilter<AuthorizeOperationFilter>();
    }
});
builder.Services.AddCors(options => options.AddPolicy(LocalCorsPolicy, policy =>
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddDbContext<AlojamientoDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions => sqlOptions.CommandTimeout(0)));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ISucursalRepository, SucursalRepository>();
builder.Services.AddScoped<ITipoHabitacionRepository, TipoHabitacionRepository>();
builder.Services.AddScoped<ITipoHabitacionImagenRepository, TipoHabitacionImagenRepository>();
builder.Services.AddScoped<ITipoHabitacionCatalogoRepository, TipoHabitacionCatalogoRepository>();
builder.Services.AddScoped<IHabitacionRepository, HabitacionRepository>();
builder.Services.AddScoped<ITarifaRepository, TarifaRepository>();
builder.Services.AddScoped<ICatalogoServicioRepository, CatalogoServicioRepository>();
builder.Services.AddScoped<IValoracionRepository, ValoracionRepository>();
builder.Services.AddScoped<ISucursalDataService, SucursalDataService>();
builder.Services.AddScoped<ITipoHabitacionDataService, TipoHabitacionDataService>();
builder.Services.AddScoped<IHabitacionDataService, HabitacionDataService>();
builder.Services.AddScoped<ITarifaDataService, TarifaDataService>();
builder.Services.AddScoped<ICatalogoServicioDataService, CatalogoServicioDataService>();
builder.Services.AddScoped<IValoracionDataService, ValoracionDataService>();
builder.Services.AddScoped<ISucursalService, SucursalService>();
builder.Services.AddScoped<ITipoHabitacionService, TipoHabitacionService>();
builder.Services.AddScoped<IHabitacionService, HabitacionService>();
builder.Services.AddScoped<ITarifaService, TarifaService>();
builder.Services.AddScoped<ICatalogoServicioService, CatalogoServicioService>();
builder.Services.AddScoped<IValoracionService, ValoracionService>();
builder.Services.AddScoped<IBookingAccommodationService, BookingAccommodationService>();
builder.Services.AddSingleton<IReservaAvailabilityClient>(sp =>
{
    var baseUrl = builder.Configuration["Services:ReservasGrpcUrl"]
        ?? builder.Configuration["Services:ReservasBaseUrl"];

    if (string.IsNullOrWhiteSpace(baseUrl))
        return new NullReservaAvailabilityClient();

    var channel = CreateReservasGrpcChannel(baseUrl);
    return new GrpcReservaAvailabilityClient(
        new ReservaGrpc.ReservaGrpcClient(channel),
        sp.GetRequiredService<ILogger<GrpcReservaAvailabilityClient>>());
});
AddJwtAuthentication(builder.Services, builder.Configuration, builder.Environment, backOfficeRoles);
if (disableAuthorizationForTesting)
{
    builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, TestingAuthorizationMiddlewareResultHandler>();
}

var app = builder.Build();
app.Logger.LogInformation("Iniciando {Service} en ambiente {Environment}", "Microservicio.Alojamiento", app.Environment.EnvironmentName);
app.Logger.LogInformation("Swagger habilitado en /swagger y /swagger/v1/swagger.json");
app.Logger.LogInformation("Cadena de conexion DefaultConnection cargada para {Service}", "Microservicio.Alojamiento");
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("v1/swagger.json", "Microservicio Alojamiento API v1");
    options.RoutePrefix = "swagger";
});
app.UseMiddleware<Alojamiento.API.Middleware.ExceptionHandlingMiddleware>();
if (app.Configuration.GetValue("HttpsRedirection:Enabled", !app.Environment.IsDevelopment()))
{
    app.UseHttpsRedirection();
}
app.UseCors(LocalCorsPolicy);
app.UseGrpcWeb();
app.UseAuthentication();
app.UseMiddleware<AdminProfileAccessMiddleware>();
app.UseAuthorization();
app.MapGet("/", () => Results.Redirect("swagger"));
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Microservicio.Alojamiento" }));
app.MapGet("/health/db", async (AlojamientoDbContext dbContext, ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("HealthDb");
    try
    {
        logger.LogInformation("Validando conexion a base de datos de Alojamiento");
        await dbContext.Database.OpenConnectionAsync();
        await dbContext.Database.CloseConnectionAsync();
        return Results.Ok(new { status = "ok", database = "connected", service = "Microservicio.Alojamiento" });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error validando conexion a base de datos de Alojamiento");
        return Results.Problem(
            detail: GetDeepestExceptionMessage(ex),
            title: "No fue posible conectar a la base de datos de Alojamiento.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});
app.MapControllers();
app.MapGrpcService<AlojamientoGrpcService>().EnableGrpcWeb();
app.Logger.LogInformation("{Service} listo para recibir solicitudes", "Microservicio.Alojamiento");
app.Run();

static void ConfigureHttpEndpoint(WebApplicationBuilder builder)
{
    var configuredPort = builder.Configuration.GetValue<int?>("Ports:Http");
    if (configuredPort is null)
        return;

    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(configuredPort.Value, listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
        });
    });
}

static GrpcChannel CreateReservasGrpcChannel(string baseUrl)
{
    var uri = new Uri(baseUrl);
    if (string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
    {
        return GrpcChannel.ForAddress(uri, new GrpcChannelOptions
        {
            HttpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler())
        });
    }

    return GrpcChannel.ForAddress(uri);
}

static string ResolveDefaultConnectionString(IConfiguration configuration, IWebHostEnvironment environment)
{
    var configuredConnectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(configuredConnectionString))
        throw new InvalidOperationException("La cadena de conexion 'DefaultConnection' es obligatoria.");

    if (HasPassword(configuredConnectionString))
        return configuredConnectionString;

    var fileConfiguration = new ConfigurationBuilder()
        .SetBasePath(environment.ContentRootPath)
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
        .Build();

    var fileConnectionString = fileConfiguration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrWhiteSpace(fileConnectionString) && HasPassword(fileConnectionString))
        return fileConnectionString;

    throw new InvalidOperationException(
        "La cadena de conexion 'DefaultConnection' esta incompleta: falta Password/Pwd. Revise la configuracion del App Service o appsettings.json.");
}

static bool HasPassword(string connectionString)
{
    try
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        return !string.IsNullOrWhiteSpace(builder.Password);
    }
    catch (ArgumentException)
    {
        return connectionString.Contains("Password=", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("Pwd=", StringComparison.OrdinalIgnoreCase);
    }
}

static string GetDeepestExceptionMessage(Exception exception)
{
    var current = exception;
    while (current.InnerException is not null)
    {
        current = current.InnerException;
    }

    return current.Message;
}

static void AddJwtAuthentication(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment, string[] backOfficeRoles)
{
    var secret = configuration["Jwt:Secret"] ?? throw new InvalidOperationException("La configuracion 'Jwt:Secret' es obligatoria.");
    var issuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("La configuracion 'Jwt:Issuer' es obligatoria.");
    var audience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("La configuracion 'Jwt:Audience' es obligatoria.");

    if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
        throw new InvalidOperationException("La configuracion 'Jwt:Secret' debe tener al menos 32 caracteres.");

    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !environment.IsDevelopment();
        options.SaveToken = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret)),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtBearer");
                logger.LogWarning("JWT auth failed: {Error}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                context.HandleResponse();

                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    return context.Response.WriteAsync(JsonSerializer.Serialize(new
                    {
                        success = false,
                        message = "No autorizado. Se requiere token de autenticacion valido.",
                        statusCode = StatusCodes.Status401Unauthorized,
                        errors = (object?)null,
                        traceId = context.HttpContext.TraceIdentifier,
                        timestamp = DateTime.UtcNow
                    }));
                }

                return Task.CompletedTask;
            }
        };
    });

    services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminProfile", policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context => !context.User.IsInRole("CLIENTE"));
        });

        options.AddPolicy("BackOffice", policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireRole(backOfficeRoles);
        });
    });
}

static void AddBearerSecurity(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions options)
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header, Description = "Ingrese el token JWT con el formato: Bearer {token}" });
}

sealed class AuthorizeOperationFilter : Swashbuckle.AspNetCore.SwaggerGen.IOperationFilter
{
    public void Apply(OpenApiOperation operation, Swashbuckle.AspNetCore.SwaggerGen.OperationFilterContext context)
    {
        if (context.ApiDescription.ActionDescriptor is not ControllerActionDescriptor descriptor)
            return;

        var hasAllowAnonymous = descriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any()
            || descriptor.MethodInfo.GetCustomAttributes(true).OfType<IAllowAnonymous>().Any()
            || descriptor.ControllerTypeInfo.GetCustomAttributes(true).OfType<IAllowAnonymous>().Any();

        if (hasAllowAnonymous)
            return;

        var hasAuthorize = descriptor.EndpointMetadata.OfType<IAuthorizeData>().Any()
            || descriptor.MethodInfo.GetCustomAttributes(true).OfType<IAuthorizeData>().Any()
            || descriptor.ControllerTypeInfo.GetCustomAttributes(true).OfType<IAuthorizeData>().Any();

        if (!hasAuthorize)
            return;

        operation.Security ??= new List<OpenApiSecurityRequirement>();
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
        });
    }
}

sealed class AccommodationDetailSwaggerCleanupFilter : Swashbuckle.AspNetCore.SwaggerGen.IOperationFilter
{
    private static readonly HashSet<string> PathsWithoutQueryApiVersion = new(StringComparer.OrdinalIgnoreCase)
    {
        "api/v1/accommodations/{sucursalGuid}",
        "api/v1/accommodations/search",
        "api/v1/accommodations/{sucursalGuid}/reviews"
    };

    public void Apply(OpenApiOperation operation, Swashbuckle.AspNetCore.SwaggerGen.OperationFilterContext context)
    {
        var relativePath = context.ApiDescription.RelativePath?.TrimEnd('/');
        if (relativePath is null || !PathsWithoutQueryApiVersion.Contains(relativePath))
            return;

        if (operation.Parameters is null)
            return;

        for (var index = operation.Parameters.Count - 1; index >= 0; index--)
        {
            var parameter = operation.Parameters[index];
            if (string.Equals(parameter.Name, "api-version", StringComparison.OrdinalIgnoreCase)
                && parameter.In == ParameterLocation.Query)
            {
                operation.Parameters.RemoveAt(index);
            }
        }
    }
}

sealed class AccommodationDetailSchemaFilter : Swashbuckle.AspNetCore.SwaggerGen.ISchemaFilter
{
    public void Apply(OpenApiSchema schema, Swashbuckle.AspNetCore.SwaggerGen.SchemaFilterContext context)
    {
        if (context.Type.FullName != "Alojamiento.Business.DTOs.Booking.AccommodationDetailResponseDTO")
            return;

        schema.Properties.Remove("disponibilidad");
    }
}

sealed class TestingAuthorizationMiddlewareResultHandler(IConfiguration configuration) : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (configuration.GetValue<bool>("Security:DisableAuthorizationForTesting"))
            return next(context);

        return _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}

sealed class AdminProfileAccessMiddleware(RequestDelegate next)
{
    private static readonly string[] BackOfficeRoles =
    [
        "ADMINISTRADOR",
        "ADMIN",
        "RECEPCIONISTA",
        "OPERATIVO",
        "DESK_SERVICE"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        if (!RequiresAdminProfile(context.Request.Path))
        {
            await next(context);
            return;
        }

        if (context.User?.Identity?.IsAuthenticated != true)
        {
            await WriteErrorAsync(context, StatusCodes.Status401Unauthorized, "No autorizado. Se requiere autenticacion.");
            return;
        }

        var roles = context.User.Claims
            .Where(claim => claim.Type == ClaimTypes.Role)
            .Select(claim => claim.Value)
            .ToList();

        var isCliente = roles.Any(role => string.Equals(role, "CLIENTE", StringComparison.OrdinalIgnoreCase));
        var hasBackOfficeRole = roles.Any(role =>
            BackOfficeRoles.Any(allowed => string.Equals(role, allowed, StringComparison.OrdinalIgnoreCase)));

        if (!hasBackOfficeRole)
        {
            await WriteErrorAsync(context, StatusCodes.Status403Forbidden, "Acceso denegado. Se requiere rol administrativo o de recepcion.");
            return;
        }

        if (isCliente)
        {
            await WriteErrorAsync(context, StatusCodes.Status403Forbidden, "Acceso denegado. El rol CLIENTE no puede ingresar al perfil administrativo.");
            return;
        }

        await next(context);
    }

    private static bool RequiresAdminProfile(PathString path)
    {
        var value = path.Value ?? string.Empty;
        return value.Contains("/internal/", StringComparison.OrdinalIgnoreCase)
            && !value.Contains("/internal/auth/", StringComparison.OrdinalIgnoreCase);
    }

    private static Task WriteErrorAsync(HttpContext context, int statusCode, string message)
    {
        if (context.Response.HasStarted)
            return Task.CompletedTask;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        return context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            success = false,
            message,
            statusCode,
            errors = (object?)null,
            traceId = context.TraceIdentifier,
            timestamp = DateTime.UtcNow
        }));
    }
}
