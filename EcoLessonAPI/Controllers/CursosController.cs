using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using EcoLessonAPI.Data;
using EcoLessonAPI.DTOs;
using EcoLessonAPI.Models;

namespace EcoLessonAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class CursosController : ControllerBase
{
    private readonly EcoLessonDbContext _context;
    private readonly ILogger<CursosController> _logger;

    public CursosController(EcoLessonDbContext context, ILogger<CursosController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponseDTO<CursoDTO>>> GetCursos(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var totalCount = await _context.Cursos.CountAsync();
        var cursos = await _context.Cursos
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CursoDTO
            {
                IdCurso = c.IdCurso,
                NomeCurso = c.NomeCurso,
                Descricao = c.Descricao,
                QtHoras = c.QtHoras
            })
            .ToListAsync();

        _logger.LogInformation("Listagem de cursos - Página {Page}, Tamanho {PageSize}", page, pageSize);

        return Ok(new PagedResponseDTO<CursoDTO>
        {
            Data = cursos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CursoDTO>> GetCurso(decimal id)
    {
        var curso = await _context.Cursos.FindAsync(id);
        if (curso == null)
        {
            _logger.LogWarning("Curso não encontrado: {Id}", id);
            return NotFound();
        }

        var cursoDto = new CursoDTO
        {
            IdCurso = curso.IdCurso,
            NomeCurso = curso.NomeCurso,
            Descricao = curso.Descricao,
            QtHoras = curso.QtHoras,
            Links = new List<LinkDTO>
            {
                new LinkDTO { Href = $"/api/v1/cursos/{curso.IdCurso}", Rel = "self", Method = "GET" },
                new LinkDTO { Href = $"/api/v1/cursos/{curso.IdCurso}", Rel = "update", Method = "PUT" },
                new LinkDTO { Href = $"/api/v1/cursos/{curso.IdCurso}", Rel = "delete", Method = "DELETE" }
            }
        };

        _logger.LogInformation("Curso recuperado: {Id}", id);
        return Ok(cursoDto);
    }

    [HttpPost]
    public async Task<ActionResult<CursoDTO>> CreateCurso([FromBody] CursoCreateDTO createDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var curso = new Curso
        {
            NomeCurso = createDto.NomeCurso,
            Descricao = createDto.Descricao,
            QtHoras = createDto.QtHoras
        };

        try
        {
            _context.Cursos.Add(curso);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Curso criado: {Id}", curso.IdCurso);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _logger.LogError(ex, "Erro ao criar curso");
            
            // Verificar se é erro de constraint única
            if (ex.InnerException is Oracle.ManagedDataAccess.Client.OracleException oracleEx)
            {
                if (oracleEx.Number == 1) // ORA-00001: unique constraint violated
                {
                    return BadRequest(new { message = "Violação de constraint única" });
                }
            }
            
            return StatusCode(500, new { message = "Erro ao criar curso. Verifique os logs para mais detalhes." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao criar curso");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }

        var cursoDto = new CursoDTO
        {
            IdCurso = curso.IdCurso,
            NomeCurso = curso.NomeCurso,
            Descricao = curso.Descricao,
            QtHoras = curso.QtHoras,
            Links = new List<LinkDTO>
            {
                new LinkDTO { Href = $"/api/v1/cursos/{curso.IdCurso}", Rel = "self", Method = "GET" }
            }
        };

        return CreatedAtAction(nameof(GetCurso), new { id = curso.IdCurso }, cursoDto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCurso(decimal id, [FromBody] CursoUpdateDTO updateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var curso = await _context.Cursos.FindAsync(id);
        if (curso == null)
        {
            return NotFound();
        }

        curso.NomeCurso = updateDto.NomeCurso;
        curso.Descricao = updateDto.Descricao;
        curso.QtHoras = updateDto.QtHoras;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Curso atualizado: {Id}", id);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCurso(decimal id)
    {
        var curso = await _context.Cursos.FindAsync(id);
        if (curso == null)
        {
            return NotFound();
        }

        _context.Cursos.Remove(curso);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Curso deletado: {Id}", id);
        return NoContent();
    }
}

