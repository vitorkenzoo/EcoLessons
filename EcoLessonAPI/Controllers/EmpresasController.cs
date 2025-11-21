using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcoLessonAPI.Data;
using EcoLessonAPI.DTOs;
using EcoLessonAPI.Models;

namespace EcoLessonAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class EmpresasController : ControllerBase
{
    private readonly EcoLessonDbContext _context;
    private readonly ILogger<EmpresasController> _logger;

    public EmpresasController(EcoLessonDbContext context, ILogger<EmpresasController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponseDTO<EmpresaDTO>>> GetEmpresas(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var totalCount = await _context.Empresas.CountAsync();
        var empresas = await _context.Empresas
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EmpresaDTO
            {
                IdEmpresa = e.IdEmpresa,
                RazaoSocial = e.RazaoSocial,
                Cnpj = e.Cnpj,
                EmailEmpresa = e.EmailEmpresa
            })
            .ToListAsync();

        _logger.LogInformation("Listagem de empresas - Página {Page}, Tamanho {PageSize}", page, pageSize);

        return Ok(new PagedResponseDTO<EmpresaDTO>
        {
            Data = empresas,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EmpresaDTO>> GetEmpresa(decimal id)
    {
        var empresa = await _context.Empresas.FindAsync(id);
        if (empresa == null)
        {
            _logger.LogWarning("Empresa não encontrada: {Id}", id);
            return NotFound();
        }

        var empresaDto = new EmpresaDTO
        {
            IdEmpresa = empresa.IdEmpresa,
            RazaoSocial = empresa.RazaoSocial,
            Cnpj = empresa.Cnpj,
            EmailEmpresa = empresa.EmailEmpresa,
            Links = new List<LinkDTO>
            {
                new LinkDTO { Href = $"/api/v1/empresas/{empresa.IdEmpresa}", Rel = "self", Method = "GET" },
                new LinkDTO { Href = $"/api/v1/empresas/{empresa.IdEmpresa}", Rel = "update", Method = "PUT" },
                new LinkDTO { Href = $"/api/v1/empresas/{empresa.IdEmpresa}", Rel = "delete", Method = "DELETE" },
                new LinkDTO { Href = $"/api/v1/vagas?empresaId={empresa.IdEmpresa}", Rel = "vagas", Method = "GET" }
            }
        };

        _logger.LogInformation("Empresa recuperada: {Id}", id);
        return Ok(empresaDto);
    }

    [HttpPost]
    public async Task<ActionResult<EmpresaDTO>> CreateEmpresa([FromBody] EmpresaCreateDTO createDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Verificar se CNPJ já existe (compatível com Oracle)
        if (await _context.Empresas.Where(e => e.Cnpj == createDto.Cnpj).CountAsync() > 0)
        {
            return BadRequest(new { message = "CNPJ já cadastrado" });
        }

        var empresa = new Empresa
        {
            RazaoSocial = createDto.RazaoSocial,
            Cnpj = createDto.Cnpj,
            EmailEmpresa = createDto.EmailEmpresa
        };

        _context.Empresas.Add(empresa);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Empresa criada: {Id}", empresa.IdEmpresa);

        var empresaDto = new EmpresaDTO
        {
            IdEmpresa = empresa.IdEmpresa,
            RazaoSocial = empresa.RazaoSocial,
            Cnpj = empresa.Cnpj,
            EmailEmpresa = empresa.EmailEmpresa,
            Links = new List<LinkDTO>
            {
                new LinkDTO { Href = $"/api/v1/empresas/{empresa.IdEmpresa}", Rel = "self", Method = "GET" }
            }
        };

        return CreatedAtAction(nameof(GetEmpresa), new { id = empresa.IdEmpresa }, empresaDto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEmpresa(decimal id, [FromBody] EmpresaUpdateDTO updateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var empresa = await _context.Empresas.FindAsync(id);
        if (empresa == null)
        {
            return NotFound();
        }

        empresa.RazaoSocial = updateDto.RazaoSocial;
        empresa.Cnpj = updateDto.Cnpj;
        empresa.EmailEmpresa = updateDto.EmailEmpresa;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Empresa atualizada: {Id}", id);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEmpresa(decimal id)
    {
        var empresa = await _context.Empresas.FindAsync(id);
        if (empresa == null)
        {
            return NotFound();
        }

        _context.Empresas.Remove(empresa);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Empresa deletada: {Id}", id);
        return NoContent();
    }
}

