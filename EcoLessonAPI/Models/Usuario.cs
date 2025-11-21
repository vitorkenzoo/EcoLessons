namespace EcoLessonAPI.Models;

public class Usuario
{
    public decimal IdUsuario { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string EmailUsuario { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
    public DateTime Cadastro { get; set; }
    public string Cpf { get; set; } = string.Empty;
    
    // Navigation properties
    public virtual ICollection<Certificado> Certificados { get; set; } = new List<Certificado>();
}

