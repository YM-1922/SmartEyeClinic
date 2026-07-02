using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartEyeClinic.Migrations
{
    /// <inheritdoc />
    public partial class FixPendingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Surgeries_Doctors",
                table: "Surgeries");

            migrationBuilder.DropForeignKey(
                name: "FK_Surgeries_Patients",
                table: "Surgeries");

            migrationBuilder.DropIndex(
                name: "IX_Surgeries_PatientId",
                table: "Surgeries");

            migrationBuilder.DropColumn(
                name: "PatientId",
                table: "Surgeries");

            migrationBuilder.AlterColumn<int>(
                name: "DoctorId",
                table: "Surgeries",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Surgeries_Doctors_DoctorId",
                table: "Surgeries",
                column: "DoctorId",
                principalTable: "Doctors",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Surgeries_Doctors_DoctorId",
                table: "Surgeries");

            migrationBuilder.AlterColumn<int>(
                name: "DoctorId",
                table: "Surgeries",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PatientId",
                table: "Surgeries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Surgeries_PatientId",
                table: "Surgeries",
                column: "PatientId");

            migrationBuilder.AddForeignKey(
                name: "FK_Surgeries_Doctors",
                table: "Surgeries",
                column: "DoctorId",
                principalTable: "Doctors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Surgeries_Patients",
                table: "Surgeries",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
