namespace EcoLessonAPI.Models;

public class Vaga
{
    public decimal IdVaga { get; set; }
    public string NomeVaga { get; set; } = string.Empty;
    public string DescricaoVaga { get; set; } = string.Empty;
    public decimal Salario { get; set; }
    public DateTime DtPublicacao { get; set; }
    public decimal IdEmpresa { get; set; }
    
    // Navigation properties
    public virtual Empresa? Empresa { get; set; }
}

