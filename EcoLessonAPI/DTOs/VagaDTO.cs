namespace EcoLessonAPI.DTOs;

public class VagaDTO
{
    public decimal IdVaga { get; set; }
    public string NomeVaga { get; set; } = string.Empty;
    public string DescricaoVaga { get; set; } = string.Empty;
    public decimal Salario { get; set; }
    public DateTime DtPublicacao { get; set; }
    public decimal IdEmpresa { get; set; }
    public string? EmpresaNome { get; set; }
    public List<LinkDTO>? Links { get; set; }
}

public class VagaCreateDTO
{
    public string NomeVaga { get; set; } = string.Empty;
    public string DescricaoVaga { get; set; } = string.Empty;
    public decimal Salario { get; set; }
    public decimal IdEmpresa { get; set; }
}

public class VagaUpdateDTO
{
    public string NomeVaga { get; set; } = string.Empty;
    public string DescricaoVaga { get; set; } = string.Empty;
    public decimal Salario { get; set; }
    public decimal IdEmpresa { get; set; }
}

