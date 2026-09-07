using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContactVerificationSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CONTACT_VERIFICATION_SESSIONS",
                columns: table => new
                {
                    ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    PURPOSE = table.Column<string>(type: "VARCHAR2(20)", maxLength: 20, nullable: false),
                    CHANNEL = table.Column<string>(type: "VARCHAR2(20)", maxLength: 20, nullable: false),
                    DESTINATION_HASH = table.Column<string>(type: "VARCHAR2(64)", maxLength: 64, nullable: false),
                    SUBJECT_USER_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: true),
                    OTP_HASH = table.Column<string>(type: "VARCHAR2(64)", maxLength: 64, nullable: true),
                    PROOF_HASH = table.Column<string>(type: "VARCHAR2(64)", maxLength: 64, nullable: true),
                    STATUS = table.Column<string>(type: "VARCHAR2(20)", maxLength: 20, nullable: false),
                    ATTEMPTS = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    EXPIRES_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    PROOF_EXPIRES_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CONTACT_VERIFICATION_SESSIONS", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CONTACT_VERIF_ACTIVE",
                table: "CONTACT_VERIFICATION_SESSIONS",
                columns: new[] { "PURPOSE", "DESTINATION_HASH", "STATUS" });

            migrationBuilder.CreateIndex(
                name: "IX_CONTACT_VERIF_PROOF",
                table: "CONTACT_VERIFICATION_SESSIONS",
                column: "PROOF_HASH");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CONTACT_VERIFICATION_SESSIONS");
        }
    }
}
