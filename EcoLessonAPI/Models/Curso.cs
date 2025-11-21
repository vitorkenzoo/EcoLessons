namespace EcoLessonAPI.Models;

public class Curso
{
    public decimal IdCurso { get; set; }
    public string NomeCurso { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal QtHoras { get; set; }
    
    // Navigation properties
    public virtual ICollection<Certificado> Certificados { get; set; } = new List<Certificado>();
}

