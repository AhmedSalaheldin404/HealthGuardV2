using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthGuard.Migrations
{
    /// <inheritdoc />
    public partial class newroleupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DoctorId",
                table: "Patients",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DoctorId",
                table: "Patients");
        }
    }
}
