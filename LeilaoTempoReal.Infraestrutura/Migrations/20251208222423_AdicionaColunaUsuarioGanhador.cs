using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeilaoTempoReal.Infraestrutura.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaColunaUsuarioGanhador : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UsuarioGanhador",
                table: "Leiloes",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UsuarioGanhador",
                table: "Leiloes");
        }
    }
}
