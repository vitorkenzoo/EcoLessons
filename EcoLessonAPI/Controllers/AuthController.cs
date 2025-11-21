using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using EcoLessonAPI.Data;
using EcoLessonAPI.DTOs;
using EcoLessonAPI.Models;
using EcoLessonAPI.Services;

namespace EcoLessonAPI.Controllers;

/// <summary>
/// Controller para autenticação e registro de usuários
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly EcoLessonDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        EcoLessonDbContext context,
        IPasswordService passwordService,
        IJwtService jwtService,
        ILogger<AuthController> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _jwtService = jwtService;
        _logger = logger;
    }

    /// <summary>
    /// Registra um novo usuário na plataforma
    /// </summary>
    /// <param name="registerDto">Dados do novo usuário</param>
    /// <returns>Dados do usuário criado com links HATEOAS</returns>
    /// <response code="201">Usuário criado com sucesso</response>
    /// <response code="400">Dados inválidos ou email/CPF já cadastrado</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UsuarioDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UsuarioDTO>> Register([FromBody] RegisterDTO registerDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Verificar se email já existe (compatível com Oracle)
        var emailExists = await _context.Usuarios
            .Where(u => u.EmailUsuario == registerDto.EmailUsuario)
            .CountAsync() > 0;
        if (emailExists)
        {
            _logger.LogWarning("Tentativa de registro com email já existente: {Email}", registerDto.EmailUsuario);
            return BadRequest(new { message = "Email já cadastrado" });
        }

        // Verificar se CPF já existe (compatível com Oracle)
        var cpfExists = await _context.Usuarios
            .Where(u => u.Cpf == registerDto.Cpf)
            .CountAsync() > 0;
        if (cpfExists)
        {
            _logger.LogWarning("Tentativa de registro com CPF já existente: {CPF}", registerDto.Cpf);
            return BadRequest(new { message = "CPF já cadastrado" });
        }

        // Hash da senha
        var hashedPassword = _passwordService.HashPassword(registerDto.Senha);

        // Criar novo usuário
        var usuario = new Usuario
        {
            Nome = registerDto.Nome,
            EmailUsuario = registerDto.EmailUsuario,
            Senha = hashedPassword,
            Cpf = registerDto.Cpf,
            Cadastro = DateTime.Now
        };

        try
        {
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Novo usuário registrado: {Email}", registerDto.EmailUsuario);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _logger.LogError(ex, "Erro ao salvar usuário: {Email}", registerDto.EmailUsuario);
            
            // Verificar se é erro de constraint única
            if (ex.InnerException is Oracle.ManagedDataAccess.Client.OracleException oracleEx)
            {
                if (oracleEx.Number == 1) // ORA-00001: unique constraint violated
                {
                    return BadRequest(new { message = "Email ou CPF já cadastrado" });
                }
            }
            
            return StatusCode(500, new { message = "Erro ao registrar usuário. Verifique os logs para mais detalhes." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao registrar usuário: {Email}", registerDto.EmailUsuario);
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }

        var usuarioDto = new UsuarioDTO
        {
            IdUsuario = usuario.IdUsuario,
            Nome = usuario.Nome,
            EmailUsuario = usuario.EmailUsuario,
            Cadastro = usuario.Cadastro,
            Cpf = usuario.Cpf,
            Links = new List<LinkDTO>
            {
                new LinkDTO { Href = $"/api/v1/usuarios/{usuario.IdUsuario}", Rel = "self", Method = "GET" }
            }
        };

        return CreatedAtAction(nameof(GetUsuario), new { id = usuario.IdUsuario }, usuarioDto);
    }

    /// <summary>
    /// Autentica um usuário e retorna um token JWT
    /// </summary>
    /// <param name="loginDto">Credenciais de login (email e senha)</param>
    /// <returns>Token JWT e data de expiração</returns>
    /// <response code="200">Login realizado com sucesso</response>
    /// <response code="400">Dados inválidos</response>
    /// <response code="401">Credenciais inválidas</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDTO>> Login([FromBody] LoginDTO loginDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.EmailUsuario == loginDto.EmailUsuario);

        if (usuario == null || !_passwordService.VerifyPassword(loginDto.Senha, usuario.Senha))
        {
            _logger.LogWarning("Tentativa de login inválida para: {Email}", loginDto.EmailUsuario);
            return Unauthorized(new { message = "Email ou senha inválidos" });
        }

        var token = _jwtService.GenerateToken(usuario);
        _logger.LogInformation("Login bem-sucedido para usuário: {Email}", loginDto.EmailUsuario);

        return Ok(new AuthResponseDTO
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60)
        });
    }

    /// <summary>
    /// Obtém dados de um usuário específico
    /// </summary>
    /// <param name="id">ID do usuário</param>
    /// <returns>Dados do usuário</returns>
    /// <response code="200">Usuário encontrado</response>
    /// <response code="404">Usuário não encontrado</response>
    [HttpGet("usuarios/{id}")]
    [ProducesResponseType(typeof(UsuarioDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UsuarioDTO>> GetUsuario(decimal id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
        {
            return NotFound();
        }

        return Ok(new UsuarioDTO
        {
            IdUsuario = usuario.IdUsuario,
            Nome = usuario.Nome,
            EmailUsuario = usuario.EmailUsuario,
            Cadastro = usuario.Cadastro,
            Cpf = usuario.Cpf
        });
    }
}

