using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoLessonAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityColumnsToAllTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create sequences for all tables
            migrationBuilder.Sql("CREATE SEQUENCE SEQ_T_USUARIOS START WITH 1 INCREMENT BY 1");
            migrationBuilder.Sql("CREATE SEQUENCE SEQ_T_EMPRESA START WITH 1 INCREMENT BY 1");
            migrationBuilder.Sql("CREATE SEQUENCE SEQ_T_VAGA START WITH 1 INCREMENT BY 1");
            migrationBuilder.Sql("CREATE SEQUENCE SEQ_T_CURSO START WITH 1 INCREMENT BY 1");
            
            // Create trigger for T_USUARIOS
            migrationBuilder.Sql(@"
                CREATE OR REPLACE TRIGGER TRG_T_USUARIOS_ID 
                BEFORE INSERT ON T_USUARIOS 
                FOR EACH ROW 
                BEGIN
                    IF :NEW.ID_USUARIO IS NULL THEN
                        SELECT SEQ_T_USUARIOS.NEXTVAL INTO :NEW.ID_USUARIO FROM DUAL;
                    END IF;
                END;
            ");
            
            // Create trigger for T_EMPRESA
            migrationBuilder.Sql(@"
                CREATE OR REPLACE TRIGGER TRG_T_EMPRESA_ID 
                BEFORE INSERT ON T_EMPRESA 
                FOR EACH ROW 
                BEGIN
                    IF :NEW.ID_EMPRESA IS NULL THEN
                        SELECT SEQ_T_EMPRESA.NEXTVAL INTO :NEW.ID_EMPRESA FROM DUAL;
                    END IF;
                END;
            ");
            
            // Create trigger for T_VAGA
            migrationBuilder.Sql(@"
                CREATE OR REPLACE TRIGGER TRG_T_VAGA_ID 
                BEFORE INSERT ON T_VAGA 
                FOR EACH ROW 
                BEGIN
                    IF :NEW.ID_VAGA IS NULL THEN
                        SELECT SEQ_T_VAGA.NEXTVAL INTO :NEW.ID_VAGA FROM DUAL;
                    END IF;
                END;
            ");
            
            // Create trigger for T_CURSO
            migrationBuilder.Sql(@"
                CREATE OR REPLACE TRIGGER TRG_T_CURSO_ID 
                BEFORE INSERT ON T_CURSO 
                FOR EACH ROW 
                BEGIN
                    IF :NEW.ID_CURSO IS NULL THEN
                        SELECT SEQ_T_CURSO.NEXTVAL INTO :NEW.ID_CURSO FROM DUAL;
                    END IF;
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop triggers
            migrationBuilder.Sql("DROP TRIGGER TRG_T_USUARIOS_ID");
            migrationBuilder.Sql("DROP TRIGGER TRG_T_EMPRESA_ID");
            migrationBuilder.Sql("DROP TRIGGER TRG_T_VAGA_ID");
            migrationBuilder.Sql("DROP TRIGGER TRG_T_CURSO_ID");
            
            // Drop sequences
            migrationBuilder.Sql("DROP SEQUENCE SEQ_T_USUARIOS");
            migrationBuilder.Sql("DROP SEQUENCE SEQ_T_EMPRESA");
            migrationBuilder.Sql("DROP SEQUENCE SEQ_T_VAGA");
            migrationBuilder.Sql("DROP SEQUENCE SEQ_T_CURSO");
        }
    }
}
