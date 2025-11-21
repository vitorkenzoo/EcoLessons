namespace EcoLessonAPI.Models;

public class Empresa
{
    public decimal IdEmpresa { get; set; }
    public string RazaoSocial { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string EmailEmpresa { get; set; } = string.Empty;
    
    // Navigation properties
    public virtual ICollection<Vaga> Vagas { get; set; } = new List<Vaga>();
}

