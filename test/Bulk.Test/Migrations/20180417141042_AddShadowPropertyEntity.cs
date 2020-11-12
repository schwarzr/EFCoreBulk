using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Bulk.Test.Migrations
{
    public partial class AddShadowPropertyEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SimpleTableWithShadowProperty",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Description_de = table.Column<string>(maxLength: 200, nullable: false, defaultValue: "DEFAULT"),
                    Description_en = table.Column<string>(maxLength: 200, nullable: true),
                    Title = table.Column<string>(maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimpleTableWithShadowProperty", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SimpleTableWithShadowProperty");
        }
    }
}
