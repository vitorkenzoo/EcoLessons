namespace EcoLessonAPI.DTOs;

public class EmpresaDTO
{
    public decimal IdEmpresa { get; set; }
    public string RazaoSocial { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string EmailEmpresa { get; set; } = string.Empty;
    public List<LinkDTO>? Links { get; set; }
}

public class EmpresaCreateDTO
{
    public string RazaoSocial { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string EmailEmpresa { get; set; } = string.Empty;
}

public class EmpresaUpdateDTO
{
    public string RazaoSocial { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string EmailEmpresa { get; set; } = string.Empty;
}

