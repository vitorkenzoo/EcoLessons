using EcoLessonAPI.DTOs;

namespace EcoLessonAPI.Services;

public interface IRecomendacaoService
{
    Task<List<CursoDTO>> ObterRecomendacoesAsync(decimal idUsuario, int topN = 5);
}

