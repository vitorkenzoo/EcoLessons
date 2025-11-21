using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EcoLessonAPI.Data;
using EcoLessonAPI.DTOs;
using Xunit;

namespace EcoLessonAPI.Tests;

public class VagasControllerTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly IServiceScope _scope;
    private readonly EcoLessonDbContext _context;

    public VagasControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove o DbContext real
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<EcoLessonDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Adiciona DbContext em memória
                services.AddDbContext<EcoLessonDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid().ToString());
                });

                // Remover autenticação JWT
                var authDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(Microsoft.AspNetCore.Authentication.IAuthenticationService));
                if (authDescriptor != null)
                {
                    services.Remove(authDescriptor);
                }

                // Configurar autenticação de teste como padrão
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        "Test", options => { });
                
                // Configurar como esquema padrão
                services.Configure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                });
            });
        });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<EcoLessonDbContext>();
        
        // Seed data
        SeedDatabase();
    }

    private void SeedDatabase()
    {
        // Criar uma empresa para os testes
        var empresa = new EcoLessonAPI.Models.Empresa
        {
            IdEmpresa = 1,
            RazaoSocial = "Empresa Teste",
            Cnpj = "12345678000190",
            EmailEmpresa = "teste@empresa.com"
        };
        _context.Empresas.Add(empresa);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetVagas_Returns200AndPagedList()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/vagas?page=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResponseDTO<VagaDTO>>();
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.True(result.Page >= 1);
        Assert.True(result.PageSize > 0);
    }

    [Fact]
    public async Task PostVagas_WithInvalidData_Returns400()
    {
        // Arrange
        var vagaDto = new VagaCreateDTO
        {
            NomeVaga = "", // Nome vazio - inválido
            DescricaoVaga = "Descrição",
            Salario = 1000,
            IdEmpresa = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/vagas", vagaDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostVagas_WithValidData_Returns201()
    {
        // Arrange
        var vagaDto = new VagaCreateDTO
        {
            NomeVaga = "Desenvolvedor .NET",
            DescricaoVaga = "Vaga para desenvolvedor .NET sênior",
            Salario = 8000,
            IdEmpresa = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/vagas", vagaDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<VagaDTO>();
        Assert.NotNull(result);
        Assert.Equal(vagaDto.NomeVaga, result.NomeVaga);
        Assert.NotNull(result.Links);
    }

    [Fact]
    public async Task GetVagas_WithNonExistentId_Returns404()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/vagas/999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public void Dispose()
    {
        _context?.Database.EnsureDeleted();
        _scope?.Dispose();
        _client?.Dispose();
    }
}

// Test Authentication Handler
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Email, "test@test.com"),
            new Claim(ClaimTypes.Name, "Test User")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

