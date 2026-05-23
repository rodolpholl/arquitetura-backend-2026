using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinControl.Lancamentos.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddIdempotencyKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "idempotency_key",
                schema: "lancamentos",
                table: "lancamentos",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "idx_lancamento_idempotency_key",
                schema: "lancamentos",
                table: "lancamentos",
                column: "idempotency_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_lancamento_idempotency_key",
                schema: "lancamentos",
                table: "lancamentos");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                schema: "lancamentos",
                table: "lancamentos");
        }
    }
}
