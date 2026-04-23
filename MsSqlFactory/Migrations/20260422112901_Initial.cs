using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MsSqlFactory.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "uuids",
                columns: table => new
                {
                    uuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    order = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_uuids", x => x.uuid);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "uuids");
        }
    }
}
