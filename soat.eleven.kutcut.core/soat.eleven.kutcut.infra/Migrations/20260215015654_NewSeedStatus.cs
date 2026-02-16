using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace soat.eleven.kutcut.infra.Migrations
{
    /// <inheritdoc />
    public partial class NewSeedStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "status",
                keyColumn: "id",
                keyValue: 2,
                column: "description",
                value: "Uploaded");

            migrationBuilder.UpdateData(
                table: "status",
                keyColumn: "id",
                keyValue: 3,
                column: "description",
                value: "Em processamento");

            migrationBuilder.UpdateData(
                table: "status",
                keyColumn: "id",
                keyValue: 4,
                column: "description",
                value: "Processado com sucesso");

            migrationBuilder.InsertData(
                table: "status",
                columns: new[] { "id", "description" },
                values: new object[] { 5, "Processado com erro" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "status",
                keyColumn: "id",
                keyValue: 5);

            migrationBuilder.UpdateData(
                table: "status",
                keyColumn: "id",
                keyValue: 2,
                column: "description",
                value: "Em processamento");

            migrationBuilder.UpdateData(
                table: "status",
                keyColumn: "id",
                keyValue: 3,
                column: "description",
                value: "Processado com sucesso");

            migrationBuilder.UpdateData(
                table: "status",
                keyColumn: "id",
                keyValue: 4,
                column: "description",
                value: "Processado com erro");
        }
    }
}
