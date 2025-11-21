using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using EcoLessonAPI.Data;
using EcoLessonAPI.DTOs;
using EcoLessonAPI.Models;

namespace EcoLessonAPI.Controllers;

/// <summary>
/// Controller para gerenciamento de vagas de emprego
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class VagasController : ControllerBase
{
    private readonly EcoLessonDbContext _context;
    private readonly ILogger<VagasController> _logger;

    public VagasController(EcoLessonDbContext context, ILogger<VagasController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Lista vagas de emprego com paginação
    /// </summary>
    /// <param name="page">Número da página (padrão: 1)</param>
    /// <param name="pageSize">Itens por página (padrão: 10, máximo: 100)</param>
    /// <param name="empresaId">Filtrar por ID da empresa (opcional)</param>
    /// <returns>Lista paginada de vagas</returns>
    /// <response code="200">Lista de vagas retornada com sucesso</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponseDTO<VagaDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponseDTO<VagaDTO>>> GetVagas(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] decimal? empresaId = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var query = _context.Vagas.Include(v => v.Empresa).AsQueryable();
        
        if (empresaId.HasValue)
        {
            query = query.Where(v => v.IdEmpresa == empresaId.Value);
        }

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
                EmpresaNome = v.Empresa != null ? v.Empresa.RazaoSocial : null
            })
            .ToListAsync();

        _logger.LogInformation("Listagem de vagas - Página {Page}, Tamanho {PageSize}", page, pageSize);

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
    /// Obtém uma vaga específica por ID
    /// </summary>
    /// <param name="id">ID da vaga</param>
    /// <returns>Dados da vaga com links HATEOAS</returns>
    /// <response code="200">Vaga encontrada</response>
    /// <response code="404">Vaga não encontrada</response>
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
            _logger.LogWarning("Vaga não encontrada: {Id}", id);
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
                new LinkDTO { Href = $"/api/v1/vagas/{vaga.IdVaga}", Rel = "self", Method = "GET" },
                new LinkDTO { Href = $"/api/v1/vagas/{vaga.IdVaga}", Rel = "update", Method = "PUT" },
                new LinkDTO { Href = $"/api/v1/vagas/{vaga.IdVaga}", Rel = "delete", Method = "DELETE" },
                new LinkDTO { Href = $"/api/v1/empresas/{vaga.IdEmpresa}", Rel = "empresa", Method = "GET" }
            }
        };

        _logger.LogInformation("Vaga recuperada: {Id}", id);
        return Ok(vagaDto);
    }

    /// <summary>
    /// Cria uma nova vaga de emprego
    /// </summary>
    /// <param name="createDto">Dados da nova vaga</param>
    /// <returns>Vaga criada com links HATEOAS</returns>
    /// <response code="201">Vaga criada com sucesso</response>
    /// <response code="400">Dados inválidos ou empresa não encontrada</response>
    [HttpPost]
    [ProducesResponseType(typeof(VagaDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VagaDTO>> CreateVaga([FromBody] VagaCreateDTO createDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var empresa = await _context.Empresas.FindAsync(createDto.IdEmpresa);
        if (empresa == null)
        {
            return BadRequest(new { message = "Empresa não encontrada" });
        }

        var vaga = new Vaga
        {
            NomeVaga = createDto.NomeVaga,
            DescricaoVaga = createDto.DescricaoVaga,
            Salario = createDto.Salario,
            DtPublicacao = DateTime.Now,
            IdEmpresa = createDto.IdEmpresa
        };

        try
        {
            _context.Vagas.Add(vaga);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Vaga criada: {Id}", vaga.IdVaga);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _logger.LogError(ex, "Erro ao criar vaga");
            
            // Verificar se é erro de constraint única ou foreign key
            if (ex.InnerException is Oracle.ManagedDataAccess.Client.OracleException oracleEx)
            {
                if (oracleEx.Number == 1) // ORA-00001: unique constraint violated
                {
                    return BadRequest(new { message = "Violação de constraint única" });
                }
                if (oracleEx.Number == 2291) // ORA-02291: integrity constraint violated - parent key not found
                {
                    return BadRequest(new { message = "Empresa não encontrada" });
                }
            }
            
            return StatusCode(500, new { message = "Erro ao criar vaga. Verifique os logs para mais detalhes." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao criar vaga");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }

        var vagaDto = new VagaDTO
        {
            IdVaga = vaga.IdVaga,
            NomeVaga = vaga.NomeVaga,
            DescricaoVaga = vaga.DescricaoVaga,
            Salario = vaga.Salario,
            DtPublicacao = vaga.DtPublicacao,
            IdEmpresa = vaga.IdEmpresa,
            EmpresaNome = empresa.RazaoSocial,
            Links = new List<LinkDTO>
            {
                new LinkDTO { Href = $"/api/v1/vagas/{vaga.IdVaga}", Rel = "self", Method = "GET" }
            }
        };

        return CreatedAtAction(nameof(GetVaga), new { id = vaga.IdVaga }, vagaDto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateVaga(decimal id, [FromBody] VagaUpdateDTO updateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var vaga = await _context.Vagas.FindAsync(id);
        if (vaga == null)
        {
            return NotFound();
        }

        vaga.NomeVaga = updateDto.NomeVaga;
        vaga.DescricaoVaga = updateDto.DescricaoVaga;
        vaga.Salario = updateDto.Salario;
        vaga.IdEmpresa = updateDto.IdEmpresa;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Vaga atualizada: {Id}", id);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteVaga(decimal id)
    {
        var vaga = await _context.Vagas.FindAsync(id);
        if (vaga == null)
        {
            return NotFound();
        }

        _context.Vagas.Remove(vaga);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Vaga deletada: {Id}", id);
        return NoContent();
    }
}

