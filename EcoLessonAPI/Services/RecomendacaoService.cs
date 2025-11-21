using Microsoft.EntityFrameworkCore;
using EcoLessonAPI.Data;
using EcoLessonAPI.DTOs;
using EcoLessonAPI.Models;

namespace EcoLessonAPI.Services;

public class RecomendacaoService : IRecomendacaoService
{
    private readonly EcoLessonDbContext _context;
    private readonly ILogger<RecomendacaoService> _logger;

    public RecomendacaoService(EcoLessonDbContext context, ILogger<RecomendacaoService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<CursoDTO>> ObterRecomendacoesAsync(decimal idUsuario, int topN = 5)
    {
        try
        {
            // Buscar certificados existentes para treinar o modelo
            var certificados = await _context.Certificados
                .Include(c => c.Curso)
                .Include(c => c.Usuario)
                .ToListAsync();

            if (certificados.Count == 0)
            {
                // Se não há certificados, retornar cursos mais populares
                var cursosPopulares = await _context.Cursos
                    .OrderByDescending(c => c.Certificados.Count)
                    .Take(topN)
                    .Select(c => new CursoDTO
                    {
                        IdCurso = c.IdCurso,
                        NomeCurso = c.NomeCurso,
                        Descricao = c.Descricao,
                        QtHoras = c.QtHoras
                    })
                    .ToListAsync();
                
                return cursosPopulares;
            }

            // Sistema de recomendação baseado em filtragem colaborativa
            // Encontra usuários similares (que completaram os mesmos cursos)
            // e recomenda cursos que esses usuários completaram

            // Obter cursos que o usuário já completou
            var cursosCompletados = await _context.Certificados
                .Where(c => c.IdUsuario == idUsuario)
                .Select(c => c.IdCurso)
                .ToListAsync();

            // Encontrar usuários similares (que completaram pelo menos um curso em comum)
            var usuariosSimilares = await _context.Certificados
                .Where(c => c.IdUsuario != idUsuario && 
                           cursosCompletados.Contains(c.IdCurso))
                .Select(c => c.IdUsuario)
                .Distinct()
                .ToListAsync();

            // Obter cursos completados por usuários similares
            var cursosRecomendados = await _context.Certificados
                .Where(c => usuariosSimilares.Contains(c.IdUsuario) && 
                           !cursosCompletados.Contains(c.IdCurso))
                .GroupBy(c => c.IdCurso)
                .OrderByDescending(g => g.Count())
                .Take(topN)
                .Select(g => g.Key)
                .ToListAsync();

            // Obter detalhes dos cursos recomendados
            var recomendacoes = await _context.Cursos
                .Where(c => cursosRecomendados.Contains(c.IdCurso))
                .Select(c => new CursoDTO
                {
                    IdCurso = c.IdCurso,
                    NomeCurso = c.NomeCurso,
                    Descricao = c.Descricao,
                    QtHoras = c.QtHoras
                })
                .ToListAsync();

            // Se não houver recomendações suficientes, adicionar cursos populares
            if (recomendacoes.Count < topN)
            {
                var cursosAdicionais = await _context.Cursos
                    .Where(c => !cursosCompletados.Contains(c.IdCurso) && 
                               !cursosRecomendados.Contains(c.IdCurso))
                    .OrderByDescending(c => c.Certificados.Count)
                    .Take(topN - recomendacoes.Count)
                    .Select(c => new CursoDTO
                    {
                        IdCurso = c.IdCurso,
                        NomeCurso = c.NomeCurso,
                        Descricao = c.Descricao,
                        QtHoras = c.QtHoras
                    })
                    .ToListAsync();
                
                recomendacoes.AddRange(cursosAdicionais);
            }

            return recomendacoes.Take(topN).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar recomendações para usuário {UsuarioId}", idUsuario);
            
            // Fallback: retornar cursos aleatórios
            var cursosFallback = await _context.Cursos
                .Take(topN)
                .Select(c => new CursoDTO
                {
                    IdCurso = c.IdCurso,
                    NomeCurso = c.NomeCurso,
                    Descricao = c.Descricao,
                    QtHoras = c.QtHoras
                })
                .ToListAsync();
            
            return cursosFallback;
        }
    }

}

