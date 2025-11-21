using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcoLessonAPI.Data;
using EcoLessonAPI.DTOs;
using EcoLessonAPI.Models;
using EcoLessonAPI.Services;

namespace EcoLessonAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class UsuariosController : ControllerBase
{
    private readonly EcoLessonDbContext _context;
    private readonly ILogger<UsuariosController> _logger;

    public UsuariosController(EcoLessonDbContext context, ILogger<UsuariosController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponseDTO<UsuarioDTO>>> GetUsuarios(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var totalCount = await _context.Usuarios.CountAsync();
        var usuarios = await _context.Usuarios
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UsuarioDTO
            {
                IdUsuario = u.IdUsuario,
                Nome = u.Nome,
                EmailUsuario = u.EmailUsuario,
                Cadastro = u.Cadastro,
                Cpf = u.Cpf
            })
            .ToListAsync();

        _logger.LogInformation("Listagem de usuários - Página {Page}, Tamanho {PageSize}", page, pageSize);

        return Ok(new PagedResponseDTO<UsuarioDTO>
        {
            Data = usuarios,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UsuarioDTO>> GetUsuario(decimal id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
        {
            _logger.LogWarning("Usuário não encontrado: {Id}", id);
            return NotFound();
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
                new LinkDTO { Href = $"/api/v1/usuarios/{usuario.IdUsuario}", Rel = "self", Method = "GET" },
                new LinkDTO { Href = $"/api/v1/usuarios/{usuario.IdUsuario}", Rel = "update", Method = "PUT" },
                new LinkDTO { Href = $"/api/v1/usuarios/{usuario.IdUsuario}", Rel = "delete", Method = "DELETE" },
                new LinkDTO { Href = $"/api/v1/usuarios/{usuario.IdUsuario}/recomendacoes-cursos", Rel = "recomendacoes", Method = "GET" }
            }
        };

        _logger.LogInformation("Usuário recuperado: {Id}", id);
        return Ok(usuarioDto);
    }

    [HttpPost]
    public async Task<ActionResult<UsuarioDTO>> CreateUsuario([FromBody] UsuarioCreateDTO createDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Verificar se email já existe (compatível com Oracle)
        if (await _context.Usuarios.Where(u => u.EmailUsuario == createDto.EmailUsuario).CountAsync() > 0)
        {
            return BadRequest(new { message = "Email já cadastrado" });
        }

        // Verificar se CPF já existe (compatível com Oracle)
        if (await _context.Usuarios.Where(u => u.Cpf == createDto.Cpf).CountAsync() > 0)
        {
            return BadRequest(new { message = "CPF já cadastrado" });
        }

        var usuario = new Usuario
        {
            Nome = createDto.Nome,
            EmailUsuario = createDto.EmailUsuario,
            Senha = createDto.Senha, // Em produção, deve ser hasheada
            Cpf = createDto.Cpf,
            Cadastro = DateTime.Now
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Usuário criado: {Id}", usuario.IdUsuario);

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

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUsuario(decimal id, [FromBody] UsuarioUpdateDTO updateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
        {
            return NotFound();
        }

        usuario.Nome = updateDto.Nome;
        usuario.EmailUsuario = updateDto.EmailUsuario;
        usuario.Cpf = updateDto.Cpf;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Usuário atualizado: {Id}", id);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUsuario(decimal id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
        {
            return NotFound();
        }

        _context.Usuarios.Remove(usuario);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Usuário deletado: {Id}", id);
        return NoContent();
    }
}

