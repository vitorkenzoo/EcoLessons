using EcoLessonAPI.Models;

namespace EcoLessonAPI.Services;

public interface IJwtService
{
    string GenerateToken(Usuario usuario);
}

