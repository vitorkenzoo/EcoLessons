using Microsoft.EntityFrameworkCore;
using EcoLessonAPI.Models;

namespace EcoLessonAPI.Data;

public class EcoLessonDbContext : DbContext
{
    public EcoLessonDbContext(DbContextOptions<EcoLessonDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Empresa> Empresas { get; set; }
    public DbSet<Vaga> Vagas { get; set; }
    public DbSet<Curso> Cursos { get; set; }
    public DbSet<Certificado> Certificados { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Usuario
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("T_USUARIOS");
            entity.HasKey(e => e.IdUsuario);
            entity.Property(e => e.IdUsuario)
                .HasColumnName("ID_USUARIO")
                .HasColumnType("NUMBER")
                .ValueGeneratedOnAdd()
                .UseIdentityColumn();
            entity.Property(e => e.Nome)
                .HasColumnName("NOME")
                .HasMaxLength(255)
                .IsRequired();
            entity.Property(e => e.EmailUsuario)
                .HasColumnName("EMAIL_USUARIO")
                .HasMaxLength(255)
                .IsRequired();
            entity.Property(e => e.Senha)
                .HasColumnName("SENHA")
                .HasMaxLength(255)
                .IsRequired();
            entity.Property(e => e.Cadastro)
                .HasColumnName("CADASTRO")
                .HasColumnType("DATE");
            entity.Property(e => e.Cpf)
                .HasColumnName("CPF")
                .HasMaxLength(14)
                .IsRequired();
            
            entity.HasIndex(e => e.EmailUsuario).IsUnique();
            entity.HasIndex(e => e.Cpf).IsUnique();
        });

        // Configure Empresa
        modelBuilder.Entity<Empresa>(entity =>
        {
            entity.ToTable("T_EMPRESA");
            entity.HasKey(e => e.IdEmpresa);
            entity.Property(e => e.IdEmpresa)
                .HasColumnName("ID_EMPRESA")
                .HasColumnType("NUMBER")
                .ValueGeneratedOnAdd()
                .UseIdentityColumn();
            entity.Property(e => e.RazaoSocial)
                .HasColumnName("RAZAO_SOCIAL")
                .HasMaxLength(255)
                .IsRequired();
            entity.Property(e => e.Cnpj)
                .HasColumnName("CNPJ")
                .HasMaxLength(18)
                .IsRequired();
            entity.Property(e => e.EmailEmpresa)
                .HasColumnName("EMAIL_EMPRESA")
                .HasMaxLength(255);
            
            entity.HasIndex(e => e.Cnpj).IsUnique();
        });

        // Configure Vaga
        modelBuilder.Entity<Vaga>(entity =>
        {
            entity.ToTable("T_VAGA");
            entity.HasKey(e => e.IdVaga);
            entity.Property(e => e.IdVaga)
                .HasColumnName("ID_VAGA")
                .HasColumnType("NUMBER")
                .ValueGeneratedOnAdd()
                .UseIdentityColumn();
            entity.Property(e => e.NomeVaga)
                .HasColumnName("NOME_VAGA")
                .HasMaxLength(255)
                .IsRequired();
            entity.Property(e => e.DescricaoVaga)
                .HasColumnName("DESCRICAO_VAGA")
                .HasColumnType("CLOB");
            entity.Property(e => e.Salario)
                .HasColumnName("SALARIO")
                .HasColumnType("NUMBER(10,2)");
            entity.Property(e => e.DtPublicacao)
                .HasColumnName("DT_PUBLICACAO")
                .HasColumnType("DATE");
            entity.Property(e => e.IdEmpresa)
                .HasColumnName("ID_EMPRESA")
                .HasColumnType("NUMBER");
            
            entity.HasOne(e => e.Empresa)
                .WithMany(e => e.Vagas)
                .HasForeignKey(e => e.IdEmpresa)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Curso
        modelBuilder.Entity<Curso>(entity =>
        {
            entity.ToTable("T_CURSO");
            entity.HasKey(e => e.IdCurso);
            entity.Property(e => e.IdCurso)
                .HasColumnName("ID_CURSO")
                .HasColumnType("NUMBER")
                .ValueGeneratedOnAdd()
                .UseIdentityColumn();
            entity.Property(e => e.NomeCurso)
                .HasColumnName("NOME_CURSO")
                .HasMaxLength(255)
                .IsRequired();
            entity.Property(e => e.Descricao)
                .HasColumnName("DESCRICAO")
                .HasColumnType("CLOB");
            entity.Property(e => e.QtHoras)
                .HasColumnName("QT_HORAS")
                .HasColumnType("NUMBER");
        });

        // Configure Certificado
        modelBuilder.Entity<Certificado>(entity =>
        {
            entity.ToTable("T_CERTIFICADO");
            entity.HasKey(e => e.IdCertificado);
            entity.Property(e => e.IdCertificado)
                .HasColumnName("ID_CERTIFICADO")
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(e => e.DtEmissao)
                .HasColumnName("DT_EMISSAO")
                .HasColumnType("DATE");
            entity.Property(e => e.Descricao)
                .HasColumnName("DESCRICAO")
                .HasMaxLength(500);
            entity.Property(e => e.CodigoValidacao)
                .HasColumnName("CODIGO_VALIDACAO")
                .HasMaxLength(100);
            entity.Property(e => e.IdUsuario)
                .HasColumnName("ID_USUARIO")
                .HasColumnType("NUMBER");
            entity.Property(e => e.IdCurso)
                .HasColumnName("ID_CURSO")
                .HasColumnType("NUMBER");
            
            entity.HasOne(e => e.Usuario)
                .WithMany(u => u.Certificados)
                .HasForeignKey(e => e.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Curso)
                .WithMany(c => c.Certificados)
                .HasForeignKey(e => e.IdCurso)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

