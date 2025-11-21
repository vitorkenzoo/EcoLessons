using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcoLessonAPI.Data;
using EcoLessonAPI.DTOs;
using EcoLessonAPI.Models;

namespace EcoLessonAPI.Controllers.V2;

/// <summary>
/// Controller v2 para gerenciamento de vagas de emprego
/// Versão melhorada com filtros adicionais e ordenação
/// </summary>
[ApiController]
[Route("api/v2/[controller]")]
[Authorize]
[Produces("application/json")]
public class VagasV2Controller : ControllerBase
{
    private readonly EcoLessonDbContext _context;
    private readonly ILogger<VagasV2Controller> _logger;

    public VagasV2Controller(EcoLessonDbContext context, ILogger<VagasV2Controller> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Lista vagas de emprego com paginação, filtros e ordenação (v2)
    /// </summary>
    /// <param name="page">Número da página (padrão: 1)</param>
    /// <param name="pageSize">Itens por página (padrão: 10, máximo: 100)</param>
    /// <param name="empresaId">Filtrar por ID da empresa (opcional)</param>
    /// <param name="salarioMin">Salário mínimo (opcional)</param>
    /// <param name="orderBy">Ordenar por: nome, salario, data (padrão: data)</param>
    /// <returns>Lista paginada de vagas</returns>
    /// <response code="200">Lista de vagas retornada com sucesso</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponseDTO<VagaDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponseDTO<VagaDTO>>> GetVagas(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] decimal? empresaId = null,
        [FromQuery] decimal? salarioMin = null,
        [FromQuery] string orderBy = "data")
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var query = _context.Vagas.Include(v => v.Empresa).AsQueryable();
        
        if (empresaId.HasValue)
        {
            query = query.Where(v => v.IdEmpresa == empresaId.Value);
        }

        if (salarioMin.HasValue)
        {
            query = query.Where(v => v.Salario >= salarioMin.Value);
        }

        // Ordenação
        query = orderBy.ToLower() switch
        {
            "nome" => query.OrderBy(v => v.NomeVaga),
            "salario" => query.OrderByDescending(v => v.Salario),
            _ => query.OrderByDescending(v => v.DtPublicacao)
        };

        var totalCount = await query.CountAsync();
        var vagas = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new VagaDTO
            {
                IdVaga = v.IdVaga,
                NomeVaga = v.NomeVaga,
                DescricaoVaga = v.DescricaoVaga,
                Salario = v.Salario,
                DtPublicacao = v.DtPublicacao,
                IdEmpresa = v.IdEmpresa,
                EmpresaNome = v.Empresa != null ? v.Empresa.RazaoSocial : null,
                Links = new List<LinkDTO>
                {
                    new LinkDTO { Href = $"/api/v2/vagas/{v.IdVaga}", Rel = "self", Method = "GET" },
                    new LinkDTO { Href = $"/api/v2/vagas/{v.IdVaga}", Rel = "update", Method = "PUT" },
                    new LinkDTO { Href = $"/api/v2/empresas/{v.IdEmpresa}", Rel = "empresa", Method = "GET" }
                }
            })
            .ToListAsync();

        _logger.LogInformation("Listagem de vagas v2 - Página {Page}, Tamanho {PageSize}, Filtros: Empresa={EmpresaId}, SalarioMin={SalarioMin}", 
            page, pageSize, empresaId, salarioMin);

        return Ok(new PagedResponseDTO<VagaDTO>
        {
            Data = vagas,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    /// <summary>
    /// Obtém uma vaga específica (v2)
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(VagaDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VagaDTO>> GetVaga(decimal id)
    {
        var vaga = await _context.Vagas
            .Include(v => v.Empresa)
            .FirstOrDefaultAsync(v => v.IdVaga == id);

        if (vaga == null)
        {
            return NotFound();
        }

        var vagaDto = new VagaDTO
        {
            IdVaga = vaga.IdVaga,
            NomeVaga = vaga.NomeVaga,
            DescricaoVaga = vaga.DescricaoVaga,
            Salario = vaga.Salario,
            DtPublicacao = vaga.DtPublicacao,
            IdEmpresa = vaga.IdEmpresa,
            EmpresaNome = vaga.Empresa?.RazaoSocial,
            Links = new List<LinkDTO>
            {
                new LinkDTO { Href = $"/api/v2/vagas/{vaga.IdVaga}", Rel = "self", Method = "GET" },
                new LinkDTO { Href = $"/api/v2/vagas/{vaga.IdVaga}", Rel = "update", Method = "PUT" },
                new LinkDTO { Href = $"/api/v2/vagas/{vaga.IdVaga}", Rel = "delete", Method = "DELETE" },
                new LinkDTO { Href = $"/api/v2/empresas/{vaga.IdEmpresa}", Rel = "empresa", Method = "GET" }
            }
        };

        return Ok(vagaDto);
    }
}

