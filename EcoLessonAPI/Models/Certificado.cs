namespace EcoLessonAPI.Models;

public class Certificado
{
    public string IdCertificado { get; set; } = string.Empty;
    public DateTime DtEmissao { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string CodigoValidacao { get; set; } = string.Empty;
    public decimal IdUsuario { get; set; }
    public decimal IdCurso { get; set; }
    
    // Navigation properties
    public virtual Usuario? Usuario { get; set; }
    public virtual Curso? Curso { get; set; }
}

