using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using EcoLessonAPI.Data;
using EcoLessonAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EcoLesson API",
        Version = "v1",
        Description = @"
## API RESTful para Plataforma de Requalificação Profissional - Versão 1

A **EcoLesson API** é uma API RESTful desenvolvida em .NET 8 que serve como a espinha dorsal de uma plataforma de requalificação profissional. 
A plataforma conecta profissionais que buscam se adaptar ao mercado, empresas que cadastram vagas e cursos educacionais para reskilling.

### Funcionalidades Principais:
- ✅ Autenticação JWT
- ✅ CRUD completo para Usuários, Empresas, Vagas, Cursos e Certificados
- ✅ Paginação em todos os endpoints de listagem
- ✅ HATEOAS (Hypermedia as the Engine of Application State)
- ✅ Sistema de recomendação de cursos usando ML.NET
- ✅ Health Checks para monitoramento
- ✅ Observabilidade com OpenTelemetry

### Como usar:
1. Primeiro, registre um usuário em `/api/v1/auth/register`
2. Faça login em `/api/v1/auth/login` para obter o token JWT
3. Use o token no botão 'Authorize' acima para acessar os endpoints protegidos
4. Explore os endpoints disponíveis abaixo

### Versionamento:
Esta API utiliza versionamento via URL. Versões disponíveis: `/api/v1/` e `/api/v2/`.
        ",
        Contact = new OpenApiContact
        {
            Name = "EcoLesson API",
            Email = "support@ecolesson.com"
        },
        License = new OpenApiLicense
        {
            Name = "MIT",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // Swagger v2
    c.SwaggerDoc("v2", new OpenApiInfo
    {
        Title = "EcoLesson API",
        Version = "v2",
        Description = @"
## API RESTful para Plataforma de Requalificação Profissional - Versão 2

Versão melhorada da API com funcionalidades adicionais:
- ✅ Filtros avançados (busca por nome, salário mínimo)
- ✅ Ordenação personalizada
- ✅ Melhorias de performance
- ✅ HATEOAS aprimorado

### Diferenças da v1:
- Vagas: Filtro por salário mínimo e ordenação (nome, salário, data)
- Empresas: Busca por nome/razão social
- Todos os endpoints mantêm compatibilidade com v1

### Como usar:
1. Use os mesmos endpoints de autenticação da v1 (`/api/v1/auth/register` e `/api/v1/auth/login`)
2. Acesse os endpoints v2 usando `/api/v2/` no lugar de `/api/v1/`
3. Use o token JWT no botão 'Authorize' acima
        ",
        Contact = new OpenApiContact
        {
            Name = "EcoLesson API",
            Email = "support@ecolesson.com"
        },
        License = new OpenApiLicense
        {
            Name = "MIT",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // Configure JWT in Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header usando o esquema Bearer. 
                      Digite apenas o token (sem 'Bearer').
                      Exemplo: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Incluir comentários XML se existirem
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Configurar para usar camelCase
    c.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
});

// Configure Entity Framework Core with Oracle
builder.Services.AddDbContext<EcoLessonDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLongForSecurity!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "EcoLessonAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "EcoLessonAPI";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromMinutes(5) // Tolerância de 5 minutos para diferenças de relógio
    };
    
    // Habilitar eventos para debug
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(context.Exception, "Falha na autenticação JWT");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Token JWT validado com sucesso para: {User}", context.Principal?.Identity?.Name);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// Register custom services
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IRecomendacaoService, RecomendacaoService>();

// Configure Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<EcoLessonDbContext>("oracle-db");

// Configure Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
        tracerProviderBuilder
            .AddAspNetCoreInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddConsoleExporter())
    .WithMetrics(metricsProviderBuilder =>
        metricsProviderBuilder
            .AddAspNetCoreInstrumentation()
            .AddConsoleExporter());

builder.Logging.AddOpenTelemetry(options =>
{
    options.AddConsoleExporter();
});

var app = builder.Build();

// Configure the HTTP request pipeline
// Swagger sempre disponível para demonstração
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EcoLesson API v1");
    c.SwaggerEndpoint("/swagger/v2/swagger.json", "EcoLesson API v2");
    c.RoutePrefix = string.Empty; // Swagger na raiz
    c.DocumentTitle = "EcoLesson API Documentation";
    c.DefaultModelsExpandDepth(-1); // Ocultar modelos por padrão
    c.DisplayRequestDuration();
    c.EnableDeepLinking();
    c.EnableFilter();
    c.ShowExtensions();
});

app.UseHttpsRedirection();

// Health Check endpoints com informações detalhadas
app.MapHealthChecks("/healthz", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds,
                exception = e.Value.Exception?.Message
            })
        });
        await context.Response.WriteAsync(result);
    }
});

app.MapHealthChecks("/readyz", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("oracle-db"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds,
                tags = e.Value.Tags,
                exception = e.Value.Exception?.Message
            })
        });
        await context.Response.WriteAsync(result);
    }
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Make Program class accessible for testing
public partial class Program { }
