using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using System.Linq;
using EcoLessonAPI.Data;
using EcoLessonAPI.DTOs;
using EcoLessonAPI.Models;

namespace EcoLessonAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class CertificadosController : ControllerBase
{
    private readonly EcoLessonDbContext _context;
    private readonly ILogger<CertificadosController> _logger;

    public CertificadosController(EcoLessonDbContext context, ILogger<CertificadosController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponseDTO<CertificadoDTO>>> GetCertificados(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var totalCount = await _context.Certificados.CountAsync();
        var certificados = await _context.Certificados
            .Include(c => c.Usuario)
            .Include(c => c.Curso)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CertificadoDTO
            {
                IdCertificado = c.IdCertificado,
                DtEmissao = c.DtEmissao,
                Descricao = c.Descricao,
                CodigoValidacao = c.CodigoValidacao,
                IdUsuario = c.IdUsuario,
                IdCurso = c.IdCurso,
                UsuarioNome = c.Usuario != null ? c.Usuario.Nome : null,
                CursoNome = c.Curso != null ? c.Curso.NomeCurso : null
            })
            .ToListAsync();

        _logger.LogInformation("Listagem de certificados - Página {Page}, Tamanho {PageSize}", page, pageSize);

        return Ok(new PagedResponseDTO<CertificadoDTO>
        {
            Data = certificados,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CertificadoDTO>> GetCertificado(string id)
    {
        var certificado = await _context.Certificados
            .Include(c => c.Usuario)
            .Include(c => c.Curso)
            .FirstOrDefaultAsync(c => c.IdCertificado == id);
        
        if (certificado == null)
        {
            _logger.LogWarning("Certificado não encontrado: {Id}", id);
            return NotFound();
        }

        var certificadoDto = new CertificadoDTO
        {
            IdCertificado = certificado.IdCertificado,
            DtEmissao = certificado.DtEmissao,
            Descricao = certificado.Descricao,
            CodigoValidacao = certificado.CodigoValidacao,
            IdUsuario = certificado.IdUsuario,
            IdCurso = certificado.IdCurso,
            UsuarioNome = certificado.Usuario?.Nome,
            CursoNome = certificado.Curso?.NomeCurso,
            Links = new List<LinkDTO>
            {
                new LinkDTO { Href = $"/api/v1/certificados/{certificado.IdCertificado}", Rel = "self", Method = "GET" },
                new LinkDTO { Href = $"/api/v1/certificados/{certificado.IdCertificado}", Rel = "update", Method = "PUT" },
                new LinkDTO { Href = $"/api/v1/certificados/{certificado.IdCertificado}", Rel = "delete", Method = "DELETE" },
                new LinkDTO { Href = $"/api/v1/usuarios/{certificado.IdUsuario}", Rel = "usuario", Method = "GET" },
                new LinkDTO { Href = $"/api/v1/cursos/{certificado.IdCurso}", Rel = "curso", Method = "GET" }
            }
        };

        _logger.LogInformation("Certificado recuperado: {Id}", id);
        return Ok(certificadoDto);
    }

    [HttpPost]
    public async Task<ActionResult<CertificadoDTO>> CreateCertificado([FromBody] CertificadoCreateDTO createDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Validar IDs
        if (createDto.IdUsuario <= 0)
        {
            return BadRequest(new { message = "ID do usuário deve ser maior que zero" });
        }
        
        if (createDto.IdCurso <= 0)
        {
            return BadRequest(new { message = "ID do curso deve ser maior que zero" });
        }

        var usuario = await _context.Usuarios.FindAsync(createDto.IdUsuario);
        if (usuario == null)
        {
            return BadRequest(new { message = "Usuário não encontrado" });
        }

        var curso = await _context.Cursos.FindAsync(createDto.IdCurso);
        if (curso == null)
        {
            return BadRequest(new { message = "Curso não encontrado" });
        }

        // Gerar ID numérico como string para compatibilidade com triggers do Oracle
        // O trigger pode estar tentando converter ID_CERTIFICADO para NUMBER(10) ou NUMBER(12)
        // Usar apenas os últimos dígitos do timestamp + número aleatório para garantir máximo de 10 dígitos
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var timestampStr = timestamp.ToString();
        // Pegar apenas os últimos 7 dígitos do timestamp (garante unicidade por ~3 anos)
        var timestampLast7 = timestampStr.Length > 7 ? timestampStr.Substring(timestampStr.Length - 7) : timestampStr;
        var random = new Random().Next(100, 999); // 3 dígitos aleatórios
        var idCertificado = $"{timestampLast7}{random}";
        
        // Garantir que seja apenas números (sem letras)
        idCertificado = new string(idCertificado.Where(char.IsDigit).ToArray());
        
        // Limitar a 10 dígitos para garantir compatibilidade com NUMBER(10) no trigger
        // 7 dígitos do timestamp + 3 dígitos aleatórios = 10 dígitos (perfeito)
        if (idCertificado.Length > 10)
        {
            idCertificado = idCertificado.Substring(0, 10);
        }
        
        // Garantir que não está vazio e tem pelo menos 8 dígitos
        if (string.IsNullOrEmpty(idCertificado) || idCertificado.Length < 8)
        {
            // Fallback: usar timestamp completo limitado a 10 dígitos
            var fallbackTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            idCertificado = fallbackTimestamp.Length > 10 ? fallbackTimestamp.Substring(fallbackTimestamp.Length - 10) : fallbackTimestamp;
        }
        
        var codigoValidacao = Guid.NewGuid().ToString("N").Substring(0, 20).ToUpper();
        
        _logger.LogInformation("Gerando certificado com ID numérico: {IdCertificado}", idCertificado);

        var certificado = new Certificado
        {
            IdCertificado = idCertificado,
            DtEmissao = DateTime.Now, // Usar DateTime.Now (EF Core converte para DATE do Oracle)
            Descricao = createDto.Descricao ?? string.Empty,
            CodigoValidacao = codigoValidacao,
            IdUsuario = createDto.IdUsuario,
            IdCurso = createDto.IdCurso
        };

        try
        {
            _context.Certificados.Add(certificado);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Certificado criado: {Id}", certificado.IdCertificado);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _logger.LogError(ex, "Erro ao criar certificado. IdCertificado: {IdCertificado}, IdUsuario: {IdUsuario}, IdCurso: {IdCurso}", 
                idCertificado, createDto.IdUsuario, createDto.IdCurso);
            
            // Verificar se é erro de constraint única ou foreign key
            if (ex.InnerException is Oracle.ManagedDataAccess.Client.OracleException oracleEx)
            {
                _logger.LogError("Erro Oracle: Number={Number}, Message={Message}", oracleEx.Number, oracleEx.Message);
                
                if (oracleEx.Number == 1) // ORA-00001: unique constraint violated
                {
                    return BadRequest(new { message = "ID do certificado já existe" });
                }
                if (oracleEx.Number == 2291) // ORA-02291: integrity constraint violated - parent key not found
                {
                    return BadRequest(new { message = "Usuário ou curso não encontrado" });
                }
                if (oracleEx.Number == 1722) // ORA-01722: invalid number
                {
                    _logger.LogError("Erro ORA-01722 ao criar certificado. Pode ser problema com trigger no banco. Detalhes: {Message}", oracleEx.Message);
                    return BadRequest(new { 
                        message = "Erro de formato de dados no banco. Pode ser causado por trigger. Verifique os logs para mais detalhes.",
                        errorCode = "ORA-01722",
                        details = "O banco de dados está tentando converter um valor para número inválido. Isso pode ser causado por um trigger."
                    });
                }
                if (oracleEx.Number == 1438) // ORA-01438: value larger than specified precision allowed for this column
                {
                    _logger.LogError("Erro ORA-01438 ao criar certificado. O ID gerado pode ser muito grande para o trigger. Detalhes: {Message}", oracleEx.Message);
                    return BadRequest(new { 
                        message = "O ID gerado excede a precisão permitida no banco de dados. Isso pode ser causado por um trigger que converte o ID para NUMBER.",
                        errorCode = "ORA-01438",
                        details = "O valor do ID é maior que a precisão especificada para a coluna no trigger. Tente novamente."
                    });
                }
            }
            
            return StatusCode(500, new { message = "Erro ao criar certificado. Verifique os logs para mais detalhes." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao criar certificado");
            return StatusCode(500, new { message = "Erro interno do servidor" });
        }

        var certificadoDto = new CertificadoDTO
        {
            IdCertificado = certificado.IdCertificado,
            DtEmissao = certificado.DtEmissao,
            Descricao = certificado.Descricao,
            CodigoValidacao = certificado.CodigoValidacao,
            IdUsuario = certificado.IdUsuario,
            IdCurso = certificado.IdCurso,
            UsuarioNome = usuario.Nome,
            CursoNome = curso.NomeCurso,
            Links = new List<LinkDTO>
            {
                new LinkDTO { Href = $"/api/v1/certificados/{certificado.IdCertificado}", Rel = "self", Method = "GET" }
            }
        };

        return CreatedAtAction(nameof(GetCertificado), new { id = certificado.IdCertificado }, certificadoDto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCertificado(string id, [FromBody] CertificadoUpdateDTO updateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var certificado = await _context.Certificados.FindAsync(id);
        if (certificado == null)
        {
            return NotFound();
        }

        certificado.Descricao = updateDto.Descricao;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Certificado atualizado: {Id}", id);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCertificado(string id)
    {
        var certificado = await _context.Certificados.FindAsync(id);
        if (certificado == null)
        {
            return NotFound();
        }

        _context.Certificados.Remove(certificado);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Certificado deletado: {Id}", id);
        return NoContent();
    }
}

