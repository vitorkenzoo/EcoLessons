namespace EcoLessonAPI.DTOs;

public class CursoDTO
{
    public decimal IdCurso { get; set; }
    public string NomeCurso { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal QtHoras { get; set; }
    public List<LinkDTO>? Links { get; set; }
}

public class CursoCreateDTO
{
    public string NomeCurso { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal QtHoras { get; set; }
}

public class CursoUpdateDTO
{
    public string NomeCurso { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal QtHoras { get; set; }
}

