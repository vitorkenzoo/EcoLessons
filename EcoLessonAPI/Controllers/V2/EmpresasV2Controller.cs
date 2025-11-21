using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcoLessonAPI.Data;
using EcoLessonAPI.DTOs;
using EcoLessonAPI.Models;

namespace EcoLessonAPI.Controllers.V2;

/// <summary>
/// Controller v2 para gerenciamento de empresas
/// Versão melhorada com busca por nome
/// </summary>
[ApiController]
[Route("api/v2/[controller]")]
[Authorize]
[Produces("application/json")]
public class EmpresasV2Controller : ControllerBase
{
    private readonly EcoLessonDbContext _context;
    private readonly ILogger<EmpresasV2Controller> _logger;

    public EmpresasV2Controller(EcoLessonDbContext context, ILogger<EmpresasV2Controller> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Lista empresas com paginação e busca por nome (v2)
    /// </summary>
    /// <param name="page">Número da página (padrão: 1)</param>
    /// <param name="pageSize">Itens por página (padrão: 10, máximo: 100)</param>
    /// <param name="search">Buscar por nome ou razão social (opcional)</param>
    /// <returns>Lista paginada de empresas</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponseDTO<EmpresaDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponseDTO<EmpresaDTO>>> GetEmpresas(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var query = _context.Empresas.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(e => e.RazaoSocial.Contains(search) || 
                                     (e.EmailEmpresa != null && e.EmailEmpresa.Contains(search)));
        }

        var totalCount = await query.CountAsync();
        var empresas = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EmpresaDTO
            {
                IdEmpresa = e.IdEmpresa,
                RazaoSocial = e.RazaoSocial,
                Cnpj = e.Cnpj,
                EmailEmpresa = e.EmailEmpresa,
                Links = new List<LinkDTO>
                {
                    new LinkDTO { Href = $"/api/v2/empresas/{e.IdEmpresa}", Rel = "self", Method = "GET" },
                    new LinkDTO { Href = $"/api/v2/empresas/{e.IdEmpresa}", Rel = "update", Method = "PUT" },
                    new LinkDTO { Href = $"/api/v2/vagas?empresaId={e.IdEmpresa}", Rel = "vagas", Method = "GET" }
                }
            })
            .ToListAsync();

        _logger.LogInformation("Listagem de empresas v2 - Página {Page}, Tamanho {PageSize}, Busca: {Search}", 
            page, pageSize, search);

        return Ok(new PagedResponseDTO<EmpresaDTO>
        {
            Data = empresas,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    /// <summary>
    /// Obtém uma empresa específica (v2)
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EmpresaDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmpresaDTO>> GetEmpresa(decimal id)
    {
        var empresa = await _context.Empresas.FindAsync(id);
        if (empresa == null)
        {
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
                new LinkDTO { Href = $"/api/v2/empresas/{empresa.IdEmpresa}", Rel = "self", Method = "GET" },
                new LinkDTO { Href = $"/api/v2/empresas/{empresa.IdEmpresa}", Rel = "update", Method = "PUT" },
                new LinkDTO { Href = $"/api/v2/empresas/{empresa.IdEmpresa}", Rel = "delete", Method = "DELETE" },
                new LinkDTO { Href = $"/api/v2/vagas?empresaId={empresa.IdEmpresa}", Rel = "vagas", Method = "GET" }
            }
        };

        return Ok(empresaDto);
    }
}

