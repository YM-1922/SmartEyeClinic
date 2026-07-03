using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartEyeClinic.Migrations
{
    /// <inheritdoc />
    public partial class MonsterAspDeployment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DepositAmount",
                table: "Appointments",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DepositStatus",
                table: "Appointments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentDate",
                table: "Appointments",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepositAmount",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "DepositStatus",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "PaymentDate",
                table: "Appointments");
        }
    }
}
