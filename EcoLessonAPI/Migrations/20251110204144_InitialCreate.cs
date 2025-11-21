using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoLessonAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "T_CURSO",
                columns: table => new
                {
                    ID_CURSO = table.Column<decimal>(type: "NUMBER", nullable: false),
                    NOME_CURSO = table.Column<string>(type: "NVARCHAR2(255)", maxLength: 255, nullable: false),
                    DESCRICAO = table.Column<string>(type: "CLOB", nullable: false),
                    QT_HORAS = table.Column<decimal>(type: "NUMBER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_CURSO", x => x.ID_CURSO);
                });

            migrationBuilder.CreateTable(
                name: "T_EMPRESA",
                columns: table => new
                {
                    ID_EMPRESA = table.Column<decimal>(type: "NUMBER", nullable: false),
                    RAZAO_SOCIAL = table.Column<string>(type: "NVARCHAR2(255)", maxLength: 255, nullable: false),
                    CNPJ = table.Column<string>(type: "NVARCHAR2(18)", maxLength: 18, nullable: false),
                    EMAIL_EMPRESA = table.Column<string>(type: "NVARCHAR2(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_EMPRESA", x => x.ID_EMPRESA);
                });

            migrationBuilder.CreateTable(
                name: "T_USUARIOS",
                columns: table => new
                {
                    ID_USUARIO = table.Column<decimal>(type: "NUMBER", nullable: false),
                    NOME = table.Column<string>(type: "NVARCHAR2(255)", maxLength: 255, nullable: false),
                    EMAIL_USUARIO = table.Column<string>(type: "NVARCHAR2(255)", maxLength: 255, nullable: false),
                    SENHA = table.Column<string>(type: "NVARCHAR2(255)", maxLength: 255, nullable: false),
                    CADASTRO = table.Column<DateTime>(type: "DATE", nullable: false),
                    CPF = table.Column<string>(type: "NVARCHAR2(14)", maxLength: 14, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_USUARIOS", x => x.ID_USUARIO);
                });

            migrationBuilder.CreateTable(
                name: "T_VAGA",
                columns: table => new
                {
                    ID_VAGA = table.Column<decimal>(type: "NUMBER", nullable: false),
                    NOME_VAGA = table.Column<string>(type: "NVARCHAR2(255)", maxLength: 255, nullable: false),
                    DESCRICAO_VAGA = table.Column<string>(type: "CLOB", nullable: false),
                    SALARIO = table.Column<decimal>(type: "NUMBER(10,2)", nullable: false),
                    DT_PUBLICACAO = table.Column<DateTime>(type: "DATE", nullable: false),
                    ID_EMPRESA = table.Column<decimal>(type: "NUMBER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_VAGA", x => x.ID_VAGA);
                    table.ForeignKey(
                        name: "FK_T_VAGA_T_EMPRESA_ID_EMPRESA",
                        column: x => x.ID_EMPRESA,
                        principalTable: "T_EMPRESA",
                        principalColumn: "ID_EMPRESA",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "T_CERTIFICADO",
                columns: table => new
                {
                    ID_CERTIFICADO = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    DT_EMISSAO = table.Column<DateTime>(type: "DATE", nullable: false),
                    DESCRICAO = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: false),
                    CODIGO_VALIDACAO = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    ID_USUARIO = table.Column<decimal>(type: "NUMBER", nullable: false),
                    ID_CURSO = table.Column<decimal>(type: "NUMBER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_CERTIFICADO", x => x.ID_CERTIFICADO);
                    table.ForeignKey(
                        name: "FK_T_CERTIFICADO_T_CURSO_ID_CURSO",
                        column: x => x.ID_CURSO,
                        principalTable: "T_CURSO",
                        principalColumn: "ID_CURSO",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_T_CERTIFICADO_T_USUARIOS_ID_USUARIO",
                        column: x => x.ID_USUARIO,
                        principalTable: "T_USUARIOS",
                        principalColumn: "ID_USUARIO",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_T_CERTIFICADO_ID_CURSO",
                table: "T_CERTIFICADO",
                column: "ID_CURSO");

            migrationBuilder.CreateIndex(
                name: "IX_T_CERTIFICADO_ID_USUARIO",
                table: "T_CERTIFICADO",
                column: "ID_USUARIO");

            migrationBuilder.CreateIndex(
                name: "IX_T_EMPRESA_CNPJ",
                table: "T_EMPRESA",
                column: "CNPJ",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_T_USUARIOS_CPF",
                table: "T_USUARIOS",
                column: "CPF",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_T_USUARIOS_EMAIL_USUARIO",
                table: "T_USUARIOS",
                column: "EMAIL_USUARIO",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_T_VAGA_ID_EMPRESA",
                table: "T_VAGA",
                column: "ID_EMPRESA");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "T_CERTIFICADO");

            migrationBuilder.DropTable(
                name: "T_VAGA");

            migrationBuilder.DropTable(
                name: "T_CURSO");

            migrationBuilder.DropTable(
                name: "T_USUARIOS");

            migrationBuilder.DropTable(
                name: "T_EMPRESA");
        }
    }
}
