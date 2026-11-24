using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace A_2.Migrations
{
    /// <summary>
    /// 1. Convierte Encargos.Fecha y Encargos.Hora de texto (nvarchar) a 'date' y 'time',
    ///    transformando los registros existentes ("dd/MM/yyyy" y "hh:mm AM/PM" o "hh:mm a. m./p. m.").
    /// 2. Cambia a RESTRICT las llaves foráneas que antes borraban en cascada:
    ///    Herramientas -> Clasificaciones y EncargoHerramientas -> Herramientas.
    /// </summary>
    public partial class FechasTipadasYRestricciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---------- Llaves foráneas: Cascade -> Restrict ----------
            migrationBuilder.DropForeignKey(
                name: "FK_EncargoHerramientas_Herramientas_HerramientaId",
                table: "EncargoHerramientas");

            migrationBuilder.DropForeignKey(
                name: "FK_Herramientas_Clasificaciones_ClasificacionId",
                table: "Herramientas");

            // ---------- Fecha y Hora: nvarchar -> date / time ----------
            migrationBuilder.AddColumn<DateOnly>(
                name: "FechaNueva",
                table: "Encargos",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "HoraNueva",
                table: "Encargos",
                type: "time",
                nullable: true);

            // Fecha: estilo 103 = dd/mm/yyyy, 105 = dd-mm-yyyy, 23 = yyyy-mm-dd.
            // Hora: se quitan espacios (incluidos los no separables que usa .NET), puntos y se pasa a
            // mayúsculas, de modo que "09:30 a. m." y "09:30 AM" quedan como "09:30AM".
            // Si un valor no se puede interpretar se usa la fecha de hoy / 00:00 para no perder el registro.
            migrationBuilder.Sql(@"
UPDATE Encargos SET
    FechaNueva = COALESCE(
        TRY_CONVERT(date, LTRIM(RTRIM(Fecha)), 103),
        TRY_CONVERT(date, LTRIM(RTRIM(Fecha)), 105),
        TRY_CONVERT(date, LTRIM(RTRIM(Fecha)), 23),
        CAST(GETDATE() AS date)),
    HoraNueva = COALESCE(
        TRY_CONVERT(time,
            UPPER(REPLACE(REPLACE(REPLACE(REPLACE(Hora, NCHAR(160), N''), NCHAR(8239), N''), N' ', N''), N'.', N''))),
        TRY_CONVERT(time, LTRIM(RTRIM(Hora))),
        CAST('00:00' AS time));");

            migrationBuilder.DropColumn(
                name: "Fecha",
                table: "Encargos");

            migrationBuilder.DropColumn(
                name: "Hora",
                table: "Encargos");

            migrationBuilder.RenameColumn(
                name: "FechaNueva",
                table: "Encargos",
                newName: "Fecha");

            migrationBuilder.RenameColumn(
                name: "HoraNueva",
                table: "Encargos",
                newName: "Hora");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "Fecha",
                table: "Encargos",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "Hora",
                table: "Encargos",
                type: "time",
                nullable: false,
                oldClrType: typeof(TimeOnly),
                oldType: "time",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_EncargoHerramientas_Herramientas_HerramientaId",
                table: "EncargoHerramientas",
                column: "HerramientaId",
                principalTable: "Herramientas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Herramientas_Clasificaciones_ClasificacionId",
                table: "Herramientas",
                column: "ClasificacionId",
                principalTable: "Clasificaciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EncargoHerramientas_Herramientas_HerramientaId",
                table: "EncargoHerramientas");

            migrationBuilder.DropForeignKey(
                name: "FK_Herramientas_Clasificaciones_ClasificacionId",
                table: "Herramientas");

            migrationBuilder.AddColumn<string>(
                name: "FechaTexto",
                table: "Encargos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HoraTexto",
                table: "Encargos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE Encargos SET
    FechaTexto = CONVERT(nvarchar(10), Fecha, 103),
    HoraTexto = FORMAT(CAST(Hora AS datetime), 'hh:mm tt', 'en-US');");

            migrationBuilder.DropColumn(
                name: "Fecha",
                table: "Encargos");

            migrationBuilder.DropColumn(
                name: "Hora",
                table: "Encargos");

            migrationBuilder.RenameColumn(
                name: "FechaTexto",
                table: "Encargos",
                newName: "Fecha");

            migrationBuilder.RenameColumn(
                name: "HoraTexto",
                table: "Encargos",
                newName: "Hora");

            migrationBuilder.AlterColumn<string>(
                name: "Fecha",
                table: "Encargos",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Hora",
                table: "Encargos",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_EncargoHerramientas_Herramientas_HerramientaId",
                table: "EncargoHerramientas",
                column: "HerramientaId",
                principalTable: "Herramientas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Herramientas_Clasificaciones_ClasificacionId",
                table: "Herramientas",
                column: "ClasificacionId",
                principalTable: "Clasificaciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
