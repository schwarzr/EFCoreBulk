using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Bulk.Test.Migrations
{
    public partial class AddBoolFlag : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "ModificationDate",
                table: "SimpleTableWithShadowProperty",
                nullable: true,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldNullable: true,
                oldDefaultValueSql: "getdate()");

            migrationBuilder.AddColumn<bool>(
                name: "BoolFlag",
                table: "SimpleTableWithShadowProperty",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BoolFlag",
                table: "SimpleTableWithShadowProperty");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ModificationDate",
                table: "SimpleTableWithShadowProperty",
                nullable: true,
                defaultValueSql: "getdate()",
                oldClrType: typeof(DateTime),
                oldNullable: true,
                oldDefaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
