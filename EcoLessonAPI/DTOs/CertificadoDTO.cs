namespace EcoLessonAPI.DTOs;

public class CertificadoDTO
{
    public string IdCertificado { get; set; } = string.Empty;
    public DateTime DtEmissao { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string CodigoValidacao { get; set; } = string.Empty;
    public decimal IdUsuario { get; set; }
    public decimal IdCurso { get; set; }
    public string? UsuarioNome { get; set; }
    public string? CursoNome { get; set; }
    public List<LinkDTO>? Links { get; set; }
}

public class CertificadoCreateDTO
{
    public string Descricao { get; set; } = string.Empty;
    public decimal IdUsuario { get; set; }
    public decimal IdCurso { get; set; }
}

public class CertificadoUpdateDTO
{
    public string Descricao { get; set; } = string.Empty;
}

