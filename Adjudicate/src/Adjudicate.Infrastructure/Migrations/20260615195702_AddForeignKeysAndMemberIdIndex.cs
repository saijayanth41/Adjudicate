using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adjudicate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddForeignKeysAndMemberIdIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Members_PlanId",
                table: "Members",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_MemberId",
                table: "Claims",
                column: "MemberId");

            migrationBuilder.AddForeignKey(
                name: "FK_Claims_Members_MemberId",
                table: "Claims",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Members_Plans_PlanId",
                table: "Members",
                column: "PlanId",
                principalTable: "Plans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Claims_Members_MemberId",
                table: "Claims");

            migrationBuilder.DropForeignKey(
                name: "FK_Members_Plans_PlanId",
                table: "Members");

            migrationBuilder.DropIndex(
                name: "IX_Members_PlanId",
                table: "Members");

            migrationBuilder.DropIndex(
                name: "IX_Claims_MemberId",
                table: "Claims");
        }
    }
}
