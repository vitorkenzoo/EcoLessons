using System.ComponentModel.DataAnnotations;

namespace EcoLessonAPI.DTOs;

/// <summary>
/// DTO para registro de novo usuário
/// </summary>
public class RegisterDTO
{
    /// <summary>
    /// Nome completo do usuário
    /// </summary>
    /// <example>João Silva</example>
    [Required(ErrorMessage = "O nome é obrigatório")]
    [StringLength(255, ErrorMessage = "O nome deve ter no máximo 255 caracteres")]
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Email do usuário (deve ser único)
    /// </summary>
    /// <example>joao.silva@email.com</example>
    [Required(ErrorMessage = "O email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string EmailUsuario { get; set; } = string.Empty;

    /// <summary>
    /// Senha do usuário (será hasheada antes de ser armazenada)
    /// </summary>
    /// <example>senha123</example>
    [Required(ErrorMessage = "A senha é obrigatória")]
    [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres")]
    public string Senha { get; set; } = string.Empty;

    /// <summary>
    /// CPF do usuário (deve ser único)
    /// </summary>
    /// <example>12345678900</example>
    [Required(ErrorMessage = "O CPF é obrigatório")]
    [StringLength(14, ErrorMessage = "CPF inválido")]
    public string Cpf { get; set; } = string.Empty;
}

/// <summary>
/// DTO para login de usuário
/// </summary>
public class LoginDTO
{
    /// <summary>
    /// Email do usuário
    /// </summary>
    /// <example>joao.silva@email.com</example>
    [Required(ErrorMessage = "O email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string EmailUsuario { get; set; } = string.Empty;

    /// <summary>
    /// Senha do usuário
    /// </summary>
    /// <example>senha123</example>
    [Required(ErrorMessage = "A senha é obrigatória")]
    public string Senha { get; set; } = string.Empty;
}

/// <summary>
/// DTO de resposta de autenticação
/// </summary>
public class AuthResponseDTO
{
    /// <summary>
    /// Token JWT para autenticação nas requisições
    /// </summary>
    /// <example>eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...</example>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Data e hora de expiração do token
    /// </summary>
    /// <example>2024-12-10T20:00:00Z</example>
    public DateTime ExpiresAt { get; set; }
}

