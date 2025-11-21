namespace EcoLessonAPI.DTOs;

public class UsuarioDTO
{
    public decimal IdUsuario { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string EmailUsuario { get; set; } = string.Empty;
    public DateTime Cadastro { get; set; }
    public string Cpf { get; set; } = string.Empty;
    public List<LinkDTO>? Links { get; set; }
}

public class UsuarioCreateDTO
{
    public string Nome { get; set; } = string.Empty;
    public string EmailUsuario { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
}

public class UsuarioUpdateDTO
{
    public string Nome { get; set; } = string.Empty;
    public string EmailUsuario { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
}

