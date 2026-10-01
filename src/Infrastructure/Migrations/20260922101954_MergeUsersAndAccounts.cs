using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MergeUsersAndAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_USER_TOKENS_USER_ACCOUNTS_ACCOUNT_ID",
                table: "USER_TOKENS");

            // U5: backfill de datos ANTES de borrar USER_ACCOUNTS/USER_CREDENTIALS.
            // PASSWORD_CHANGED_AT se agrega ya (en vez de más abajo) para poder
            // escribirle datos reales en el mismo paso.
            migrationBuilder.AddColumn<DateTime>(
                name: "PASSWORD_CHANGED_AT",
                table: "USERS",
                type: "TIMESTAMP",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE USERS u
                SET PASSWORD_HASH = (
                        SELECT c.PASSWORD_HASH
                        FROM USER_CREDENTIALS c
                        JOIN USER_ACCOUNTS a ON a.ACCOUNT_ID = c.ACCOUNT_ID
                        WHERE a.USER_ID = u.USER_ID),
                    PASSWORD_CHANGED_AT = (
                        SELECT c.LAST_CHANGED
                        FROM USER_CREDENTIALS c
                        JOIN USER_ACCOUNTS a ON a.ACCOUNT_ID = c.ACCOUNT_ID
                        WHERE a.USER_ID = u.USER_ID)
                WHERE EXISTS (
                    SELECT 1
                    FROM USER_ACCOUNTS a
                    JOIN USER_CREDENTIALS c ON c.ACCOUNT_ID = a.ACCOUNT_ID
                    WHERE a.USER_ID = u.USER_ID)
            ");

            // Usuarios sin cuenta/credenciales previas (no deberían existir hoy,
            // pero por seguridad): hash vacío nunca verifica ninguna contraseña,
            // en vez de dejar un NULL que podría comportarse distinto según el
            // camino de código que lo lea.
            migrationBuilder.Sql(@"
                UPDATE USERS SET PASSWORD_HASH = '' WHERE PASSWORD_HASH IS NULL
            ");

            // USER_TOKENS.ACCOUNT_ID (todavía sin renombrar en este punto) tenía
            // el id de USER_ACCOUNTS; lo remapeamos al USER_ID real mientras la
            // tabla vieja todavía existe para poder hacer el JOIN. Sin este paso,
            // el RenameColumn de más abajo dejaría el valor viejo (id de cuenta)
            // bajo el nombre nuevo (USER_ID) -- las sesiones activas quedarían
            // apuntando a un id que no es de nadie.
            migrationBuilder.Sql(@"
                UPDATE USER_TOKENS t
                SET ACCOUNT_ID = (
                    SELECT a.USER_ID FROM USER_ACCOUNTS a WHERE a.ACCOUNT_ID = t.ACCOUNT_ID)
                WHERE EXISTS (
                    SELECT 1 FROM USER_ACCOUNTS a WHERE a.ACCOUNT_ID = t.ACCOUNT_ID)
            ");

            migrationBuilder.DropTable(
                name: "ACCOUNT_STATEMENTS");

            migrationBuilder.DropTable(
                name: "APPOINTMENT_ACTION_VERIFICATION_SESSIONS");

            migrationBuilder.DropTable(
                name: "CHAT_AI_RUN_ERRORS");

            migrationBuilder.DropTable(
                name: "CHAT_AI_RUN_METRICS");

            migrationBuilder.DropTable(
                name: "CHAT_ATTACHMENTS");

            migrationBuilder.DropTable(
                name: "CHAT_CONVERSATION_AI_SETTINGS");

            migrationBuilder.DropTable(
                name: "CHAT_CONVERSATION_ASSIGNMENTS");

            migrationBuilder.DropTable(
                name: "CHAT_ESCALATION_ASSIGNMENTS");

            migrationBuilder.DropTable(
                name: "CHAT_ESCALATION_STATUS_HISTORY");

            migrationBuilder.DropTable(
                name: "TELEGRAM_LINK_CODES");

            migrationBuilder.DropTable(
                name: "TELEGRAM_LINKING_SESSIONS");

            migrationBuilder.DropTable(
                name: "TELEGRAM_REGISTRATION_SESSIONS");

            migrationBuilder.DropTable(
                name: "USER_CREDENTIALS");

            migrationBuilder.DropTable(
                name: "USER_PERMISSIONS");

            migrationBuilder.DropTable(
                name: "CHAT_AI_RUNS");

            migrationBuilder.DropTable(
                name: "USER_ACCOUNTS");

            migrationBuilder.DropTable(
                name: "AI_MODELS");

            migrationBuilder.DropTable(
                name: "AI_RUNS_STATUSES");

            migrationBuilder.DropTable(
                name: "PROVIDER_MODELS_AI");

            migrationBuilder.RenameColumn(
                name: "ACCOUNT_ID",
                table: "USER_TOKENS",
                newName: "USER_ID");

            migrationBuilder.RenameIndex(
                name: "IX_USER_TOKENS_ACCOUNT_ID",
                table: "USER_TOKENS",
                newName: "IX_USER_TOKENS_USER_ID");

            migrationBuilder.AlterColumn<string>(
                name: "PASSWORD_HASH",
                table: "USERS",
                type: "VARCHAR2(255)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "VARCHAR2(255)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_USER_TOKENS_USERS_USER_ID",
                table: "USER_TOKENS",
                column: "USER_ID",
                principalTable: "USERS",
                principalColumn: "USER_ID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_USER_TOKENS_USERS_USER_ID",
                table: "USER_TOKENS");

            migrationBuilder.DropColumn(
                name: "PASSWORD_CHANGED_AT",
                table: "USERS");

            migrationBuilder.RenameColumn(
                name: "USER_ID",
                table: "USER_TOKENS",
                newName: "ACCOUNT_ID");

            migrationBuilder.RenameIndex(
                name: "IX_USER_TOKENS_USER_ID",
                table: "USER_TOKENS",
                newName: "IX_USER_TOKENS_ACCOUNT_ID");

            migrationBuilder.AlterColumn<string>(
                name: "PASSWORD_HASH",
                table: "USERS",
                type: "VARCHAR2(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "VARCHAR2(255)");

            migrationBuilder.CreateTable(
                name: "AI_RUNS_STATUSES",
                columns: table => new
                {
                    AI_RUNS_STATUSES_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    NAME_STATUS = table.Column<string>(type: "VARCHAR2(50)", maxLength: 50, nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AI_RUNS_STATUSES", x => x.AI_RUNS_STATUSES_ID);
                });

            migrationBuilder.CreateTable(
                name: "APPOINTMENT_ACTION_VERIFICATION_SESSIONS",
                columns: table => new
                {
                    ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    ACTION = table.Column<string>(type: "VARCHAR2(20)", maxLength: 20, nullable: false),
                    ACTION_PAYLOAD = table.Column<string>(type: "VARCHAR2(1000)", maxLength: 1000, nullable: true),
                    APPOINTMENT_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    ATTEMPTS = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    CHANNEL = table.Column<string>(type: "VARCHAR2(20)", maxLength: 20, nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    DESTINATION_HASH = table.Column<string>(type: "VARCHAR2(64)", maxLength: 64, nullable: false),
                    EXPIRES_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    OTP_HASH = table.Column<string>(type: "VARCHAR2(64)", maxLength: 64, nullable: true),
                    STATUS = table.Column<string>(type: "VARCHAR2(20)", maxLength: 20, nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_APPOINTMENT_ACTION_VERIFICATION_SESSIONS", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "CHAT_ATTACHMENTS",
                columns: table => new
                {
                    CHAT_ATTACHMENTS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CHAT_MESSAGES_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    FILE_NAME = table.Column<string>(type: "VARCHAR2(255)", maxLength: 255, nullable: false),
                    FILE_TYPE = table.Column<string>(type: "VARCHAR2(100)", maxLength: 100, nullable: false),
                    FILE_URL = table.Column<string>(type: "VARCHAR2(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHAT_ATTACHMENTS", x => x.CHAT_ATTACHMENTS_ID);
                    table.ForeignKey(
                        name: "FK_CHAT_ATTACHMENTS_CHAT_MESSAGES_CHAT_MESSAGES_ID",
                        column: x => x.CHAT_MESSAGES_ID,
                        principalTable: "CHAT_MESSAGES",
                        principalColumn: "CHAT_MESSAGES_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CHAT_CONVERSATION_ASSIGNMENTS",
                columns: table => new
                {
                    CHAT_CONVERSATIONS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    AGENT_HUMAN_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: true),
                    ASSIGNED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    UNASSIGNED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHAT_CONVERSATION_ASSIGNMENTS", x => x.CHAT_CONVERSATIONS_ID);
                    table.ForeignKey(
                        name: "FK_CHAT_CONVERSATION_ASSIGNMENTS_AGENT_HUMANS_AGENT_HUMAN_ID",
                        column: x => x.AGENT_HUMAN_ID,
                        principalTable: "AGENT_HUMANS",
                        principalColumn: "AGENT_HUMAN_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CHAT_CONVERSATION_ASSIGNMENTS_CHAT_CONVERSATIONS_CHAT_CONVERSATIONS_ID",
                        column: x => x.CHAT_CONVERSATIONS_ID,
                        principalTable: "CHAT_CONVERSATIONS",
                        principalColumn: "CHAT_CONVERSATIONS_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CHAT_ESCALATION_ASSIGNMENTS",
                columns: table => new
                {
                    CHAT_ESCALATION_ASSIGNMENTS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    AGENT_HUMAN_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    ASSIGNED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    CHAT_ESCALATIONS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHAT_ESCALATION_ASSIGNMENTS", x => x.CHAT_ESCALATION_ASSIGNMENTS_ID);
                    table.ForeignKey(
                        name: "FK_CHAT_ESC_ASG_AGT",
                        column: x => x.AGENT_HUMAN_ID,
                        principalTable: "AGENT_HUMANS",
                        principalColumn: "AGENT_HUMAN_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CHAT_ESC_ASG_ESC",
                        column: x => x.CHAT_ESCALATIONS_ID,
                        principalTable: "CHAT_ESCALATIONS",
                        principalColumn: "CHAT_ESCALATIONS_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CHAT_ESCALATION_STATUS_HISTORY",
                columns: table => new
                {
                    CHAT_ESCALATION_STATUS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CHAT_ESCALATIONS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    ESCALATIONS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHAT_ESC_STAT_HIST", x => x.CHAT_ESCALATION_STATUS_ID);
                    table.ForeignKey(
                        name: "FK_CHAT_ESC_HIST_ESC",
                        column: x => x.CHAT_ESCALATIONS_ID,
                        principalTable: "CHAT_ESCALATIONS",
                        principalColumn: "CHAT_ESCALATIONS_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CHAT_ESC_HIST_STA",
                        column: x => x.ESCALATIONS_ID,
                        principalTable: "ESCALATIONS_STATUSES",
                        principalColumn: "ESCALATIONS_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PROVIDER_MODELS_AI",
                columns: table => new
                {
                    PROVIDER_MODEL_AI_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    BUSINESS_NAME = table.Column<string>(type: "VARCHAR2(200)", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    IS_ACTIVE = table.Column<int>(type: "NUMBER(10)", nullable: false, defaultValue: 1),
                    NAME_PROVIDER_AI = table.Column<string>(type: "VARCHAR2(150)", maxLength: 150, nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    WEBSITE = table.Column<string>(type: "VARCHAR2(500)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PROVIDER_MODELS_AI", x => x.PROVIDER_MODEL_AI_ID);
                });

            migrationBuilder.CreateTable(
                name: "TELEGRAM_LINK_CODES",
                columns: table => new
                {
                    ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CODE_HASH = table.Column<string>(type: "VARCHAR2(64)", maxLength: 64, nullable: false),
                    CONSUMED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    EXPIRES_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    INVALIDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    PERSON_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TELEGRAM_LINK_CODES", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TELEGRAM_LINK_CODES_USERS",
                        column: x => x.PERSON_ID,
                        principalTable: "USERS",
                        principalColumn: "USER_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TELEGRAM_LINKING_SESSIONS",
                columns: table => new
                {
                    ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    ATTEMPTS = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    EMAIL_HASH = table.Column<string>(type: "VARCHAR2(64)", maxLength: 64, nullable: true),
                    EXPIRES_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    OTP_HASH = table.Column<string>(type: "VARCHAR2(64)", maxLength: 64, nullable: true),
                    PERSON_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: true),
                    STATUS = table.Column<string>(type: "VARCHAR2(20)", maxLength: 20, nullable: false),
                    TELEGRAM_CHAT_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    TELEGRAM_USER_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TELEGRAM_LINKING_SESSIONS", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TELEGRAM_LINKING_SESSIONS_USERS",
                        column: x => x.PERSON_ID,
                        principalTable: "USERS",
                        principalColumn: "USER_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TELEGRAM_REGISTRATION_SESSIONS",
                columns: table => new
                {
                    ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    ACCOUNT_KIND = table.Column<string>(type: "VARCHAR2(24)", maxLength: 24, nullable: false),
                    ATTEMPTS = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    COMPLETION_EXPIRES_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    COMPLETION_TOKEN_HASH = table.Column<string>(type: "VARCHAR2(64)", maxLength: 64, nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    EMAIL_HASH = table.Column<string>(type: "VARCHAR2(64)", maxLength: 64, nullable: true),
                    OTP_EXPIRES_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    OTP_HASH = table.Column<string>(type: "VARCHAR2(64)", maxLength: 64, nullable: true),
                    PERSON_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: true),
                    PROTECTED_EMAIL = table.Column<string>(type: "VARCHAR2(2048)", maxLength: 2048, nullable: true),
                    STATUS = table.Column<string>(type: "VARCHAR2(24)", maxLength: 24, nullable: false),
                    TELEGRAM_CHAT_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    TELEGRAM_USER_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TELEGRAM_REGISTRATION_SESSIONS", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TG_REG_SESSIONS_USERS",
                        column: x => x.PERSON_ID,
                        principalTable: "USERS",
                        principalColumn: "USER_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "USER_ACCOUNTS",
                columns: table => new
                {
                    ACCOUNT_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    MAIL = table.Column<string>(type: "VARCHAR2(150)", maxLength: 150, nullable: false),
                    STATUS = table.Column<string>(type: "VARCHAR2(40)", maxLength: 40, nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    USER_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    USERNAME = table.Column<string>(type: "VARCHAR2(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USER_ACCOUNTS", x => x.ACCOUNT_ID);
                    table.ForeignKey(
                        name: "FK_USER_ACCOUNTS_USERS_USER_ID",
                        column: x => x.USER_ID,
                        principalTable: "USERS",
                        principalColumn: "USER_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "USER_PERMISSIONS",
                columns: table => new
                {
                    USER_PERMISSION_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CAN_CREATE = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    CAN_DELETE = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    CAN_EDIT = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    CAN_VIEW = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    MODULE_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true),
                    USER_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USER_PERMISSIONS", x => x.USER_PERMISSION_ID);
                    table.ForeignKey(
                        name: "FK_USER_PERMISSIONS_MODULES_MODULE_ID",
                        column: x => x.MODULE_ID,
                        principalTable: "MODULES",
                        principalColumn: "MODULE_ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_USER_PERMISSIONS_USERS_USER_ID",
                        column: x => x.USER_ID,
                        principalTable: "USERS",
                        principalColumn: "USER_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AI_MODELS",
                columns: table => new
                {
                    AI_MODEL_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CONTEXT_WINDOW = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    INPUT_TOKEN_PRICE = table.Column<decimal>(type: "NUMBER(18,6)", nullable: false),
                    IS_ACTIVE = table.Column<int>(type: "NUMBER(10)", nullable: false, defaultValue: 1),
                    MAX_TOKENS = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    MODEL_KEY = table.Column<string>(type: "VARCHAR2(150)", nullable: false),
                    NAME_MODEL = table.Column<string>(type: "VARCHAR2(150)", nullable: false),
                    OUTPUT_TOKEN_PRICE = table.Column<decimal>(type: "NUMBER(18,6)", nullable: false),
                    PROVIDER_MODEL_AI_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AI_MODELS", x => x.AI_MODEL_ID);
                    table.ForeignKey(
                        name: "FK_AI_MODELS_PROVIDER_MODELS_AI_PROVIDER_MODEL_AI_ID",
                        column: x => x.PROVIDER_MODEL_AI_ID,
                        principalTable: "PROVIDER_MODELS_AI",
                        principalColumn: "PROVIDER_MODEL_AI_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ACCOUNT_STATEMENTS",
                columns: table => new
                {
                    STATEMENT_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    ACCOUNT_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    ISSUE_DATE = table.Column<DateTime>(type: "DATE", nullable: false),
                    STATUS = table.Column<string>(type: "VARCHAR2(30)", maxLength: 30, nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ACCOUNT_STATEMENTS", x => x.STATEMENT_ID);
                    table.ForeignKey(
                        name: "FK_ACCOUNT_STATEMENTS_USER_ACCOUNTS_ACCOUNT_ID",
                        column: x => x.ACCOUNT_ID,
                        principalTable: "USER_ACCOUNTS",
                        principalColumn: "ACCOUNT_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "USER_CREDENTIALS",
                columns: table => new
                {
                    CREDENTIAL_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    ACCOUNT_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    LAST_CHANGED = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    PASSWORD_HASH = table.Column<string>(type: "VARCHAR2(255)", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USER_CREDENTIALS", x => x.CREDENTIAL_ID);
                    table.ForeignKey(
                        name: "FK_USER_CREDENTIALS_USER_ACCOUNTS_ACCOUNT_ID",
                        column: x => x.ACCOUNT_ID,
                        principalTable: "USER_ACCOUNTS",
                        principalColumn: "ACCOUNT_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CHAT_AI_RUNS",
                columns: table => new
                {
                    CHAT_AI_RUNS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    AI_MODEL_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    AI_RUNS_STATUSES_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CHAT_CONVERSATIONS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CHAT_MESSAGES_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHAT_AI_RUNS", x => x.CHAT_AI_RUNS_ID);
                    table.ForeignKey(
                        name: "FK_CHAT_AI_RUNS_AI_MODELS_AI_MODEL_ID",
                        column: x => x.AI_MODEL_ID,
                        principalTable: "AI_MODELS",
                        principalColumn: "AI_MODEL_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CHAT_AI_RUNS_AI_RUNS_STATUSES_AI_RUNS_STATUSES_ID",
                        column: x => x.AI_RUNS_STATUSES_ID,
                        principalTable: "AI_RUNS_STATUSES",
                        principalColumn: "AI_RUNS_STATUSES_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CHAT_AI_RUNS_CHAT_CONVERSATIONS_CHAT_CONVERSATIONS_ID",
                        column: x => x.CHAT_CONVERSATIONS_ID,
                        principalTable: "CHAT_CONVERSATIONS",
                        principalColumn: "CHAT_CONVERSATIONS_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CHAT_AI_RUNS_CHAT_MESSAGES_CHAT_MESSAGES_ID",
                        column: x => x.CHAT_MESSAGES_ID,
                        principalTable: "CHAT_MESSAGES",
                        principalColumn: "CHAT_MESSAGES_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CHAT_CONVERSATION_AI_SETTINGS",
                columns: table => new
                {
                    CHAT_CONVERSATION_AI_SETTING_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    AI_ENABLED = table.Column<int>(type: "NUMBER(10)", nullable: false, defaultValue: 1),
                    CONVERSATION_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    DEFAULT_MODEL_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: true),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHAT_CONVERSATION_AI_SETTINGS", x => x.CHAT_CONVERSATION_AI_SETTING_ID);
                    table.ForeignKey(
                        name: "FK_CHAT_CONVERSATION_AI_SETTINGS_AI_MODELS_DEFAULT_MODEL_ID",
                        column: x => x.DEFAULT_MODEL_ID,
                        principalTable: "AI_MODELS",
                        principalColumn: "AI_MODEL_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CHAT_CONVERSATION_AI_SETTINGS_CHAT_CONVERSATIONS_CONVERSATION_ID",
                        column: x => x.CONVERSATION_ID,
                        principalTable: "CHAT_CONVERSATIONS",
                        principalColumn: "CHAT_CONVERSATIONS_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CHAT_AI_RUN_ERRORS",
                columns: table => new
                {
                    CHAT_AI_RUN_ERRORS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CHAT_AI_RUNS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    ERROR_CODE = table.Column<string>(type: "VARCHAR2(80)", maxLength: 80, nullable: true),
                    ERROR_MESSAGE = table.Column<string>(type: "CLOB", nullable: false),
                    PROVIDER_ERROR_ID = table.Column<string>(type: "VARCHAR2(120)", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHAT_AI_RUN_ERRORS", x => x.CHAT_AI_RUN_ERRORS_ID);
                    table.ForeignKey(
                        name: "FK_CHAT_AI_RUN_ERRORS_CHAT_AI_RUNS_CHAT_AI_RUNS_ID",
                        column: x => x.CHAT_AI_RUNS_ID,
                        principalTable: "CHAT_AI_RUNS",
                        principalColumn: "CHAT_AI_RUNS_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CHAT_AI_RUN_METRICS",
                columns: table => new
                {
                    CHAT_AI_RUN_METRICS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CHAT_AI_RUNS_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    COMPLETION_TOKENS = table.Column<int>(type: "NUMBER(10)", nullable: false, defaultValue: 0),
                    COST = table.Column<decimal>(type: "NUMBER(18,6)", nullable: false, defaultValue: 0m),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP", nullable: false),
                    PROMPT_TOKENS = table.Column<int>(type: "NUMBER(10)", nullable: false, defaultValue: 0),
                    TOTAL_TOKENS = table.Column<int>(type: "NUMBER(10)", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHAT_AI_RUN_METRICS", x => x.CHAT_AI_RUN_METRICS_ID);
                    table.ForeignKey(
                        name: "FK_CHAT_AI_RUN_METRICS_CHAT_AI_RUNS_CHAT_AI_RUNS_ID",
                        column: x => x.CHAT_AI_RUNS_ID,
                        principalTable: "CHAT_AI_RUNS",
                        principalColumn: "CHAT_AI_RUNS_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ACCOUNT_STATEMENTS_ACCOUNT_ID",
                table: "ACCOUNT_STATEMENTS",
                column: "ACCOUNT_ID");

            migrationBuilder.CreateIndex(
                name: "IX_AI_MODELS_PROVIDER_MODEL_AI_ID",
                table: "AI_MODELS",
                column: "PROVIDER_MODEL_AI_ID");

            migrationBuilder.CreateIndex(
                name: "IX_APPT_ACTION_VERIF_ACTIVE",
                table: "APPOINTMENT_ACTION_VERIFICATION_SESSIONS",
                columns: new[] { "APPOINTMENT_ID", "ACTION", "STATUS" });

            migrationBuilder.CreateIndex(
                name: "IX_APPT_ACTION_VERIF_DEST",
                table: "APPOINTMENT_ACTION_VERIFICATION_SESSIONS",
                column: "DESTINATION_HASH");

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_AI_RUN_ERRORS_CHAT_AI_RUNS_ID",
                table: "CHAT_AI_RUN_ERRORS",
                column: "CHAT_AI_RUNS_ID");

            migrationBuilder.CreateIndex(
                name: "UX_CHAT_AI_RUN_METRICS_CHAT_AI_RUNS_ID",
                table: "CHAT_AI_RUN_METRICS",
                column: "CHAT_AI_RUNS_ID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_AI_RUNS_AI_MODELS_ID",
                table: "CHAT_AI_RUNS",
                column: "AI_MODEL_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_AI_RUNS_AI_RUN_STATUSES_ID",
                table: "CHAT_AI_RUNS",
                column: "AI_RUNS_STATUSES_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_AI_RUNS_CHAT_CONVERSATIONS_ID",
                table: "CHAT_AI_RUNS",
                column: "CHAT_CONVERSATIONS_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_AI_RUNS_CHAT_CONVERSATIONS_ID_CREATED_AT",
                table: "CHAT_AI_RUNS",
                columns: new[] { "CHAT_CONVERSATIONS_ID", "CREATED_AT" });

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_AI_RUNS_CHAT_MESSAGES_ID",
                table: "CHAT_AI_RUNS",
                column: "CHAT_MESSAGES_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_ATTACHMENTS_CHAT_MESSAGES_ID",
                table: "CHAT_ATTACHMENTS",
                column: "CHAT_MESSAGES_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_CONVERSATION_AI_SETTINGS_CONVERSATION_ID",
                table: "CHAT_CONVERSATION_AI_SETTINGS",
                column: "CONVERSATION_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_CONVERSATION_AI_SETTINGS_DEFAULT_MODEL_ID",
                table: "CHAT_CONVERSATION_AI_SETTINGS",
                column: "DEFAULT_MODEL_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_CONVERSATION_ASSIGNMENTS_AGENT_HUMAN_ID",
                table: "CHAT_CONVERSATION_ASSIGNMENTS",
                column: "AGENT_HUMAN_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_ESC_ASG_AGT",
                table: "CHAT_ESCALATION_ASSIGNMENTS",
                column: "AGENT_HUMAN_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_ESC_ASG_ESC",
                table: "CHAT_ESCALATION_ASSIGNMENTS",
                column: "CHAT_ESCALATIONS_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_ESC_HIST_ESC",
                table: "CHAT_ESCALATION_STATUS_HISTORY",
                column: "CHAT_ESCALATIONS_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CHAT_ESC_HIST_STA",
                table: "CHAT_ESCALATION_STATUS_HISTORY",
                column: "ESCALATIONS_ID");

            migrationBuilder.CreateIndex(
                name: "IX_TELEGRAM_LINK_CODES_PERSON",
                table: "TELEGRAM_LINK_CODES",
                column: "PERSON_ID");

            migrationBuilder.CreateIndex(
                name: "UX_TELEGRAM_LINK_CODES_HASH",
                table: "TELEGRAM_LINK_CODES",
                column: "CODE_HASH",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TELEGRAM_LINKING_SESSIONS_ACTIVE",
                table: "TELEGRAM_LINKING_SESSIONS",
                columns: new[] { "TELEGRAM_USER_ID", "STATUS" });

            migrationBuilder.CreateIndex(
                name: "IX_TELEGRAM_LINKING_SESSIONS_EMAIL",
                table: "TELEGRAM_LINKING_SESSIONS",
                column: "EMAIL_HASH");

            migrationBuilder.CreateIndex(
                name: "IX_TELEGRAM_LINKING_SESSIONS_PERSON_ID",
                table: "TELEGRAM_LINKING_SESSIONS",
                column: "PERSON_ID");

            migrationBuilder.CreateIndex(
                name: "IX_TELEGRAM_REGISTRATION_SESSIONS_PERSON_ID",
                table: "TELEGRAM_REGISTRATION_SESSIONS",
                column: "PERSON_ID");

            migrationBuilder.CreateIndex(
                name: "IX_TG_REG_SESSIONS_ACTIVE",
                table: "TELEGRAM_REGISTRATION_SESSIONS",
                columns: new[] { "TELEGRAM_USER_ID", "STATUS" });

            migrationBuilder.CreateIndex(
                name: "IX_TG_REG_SESSIONS_EMAIL",
                table: "TELEGRAM_REGISTRATION_SESSIONS",
                column: "EMAIL_HASH");

            migrationBuilder.CreateIndex(
                name: "UX_TG_REG_SESSIONS_TOKEN",
                table: "TELEGRAM_REGISTRATION_SESSIONS",
                column: "COMPLETION_TOKEN_HASH",
                unique: true,
                filter: "\"COMPLETION_TOKEN_HASH\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_USER_ACCOUNTS_MAIL",
                table: "USER_ACCOUNTS",
                column: "MAIL",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_USER_ACCOUNTS_USER_ID",
                table: "USER_ACCOUNTS",
                column: "USER_ID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_USER_ACCOUNTS_USERNAME",
                table: "USER_ACCOUNTS",
                column: "USERNAME",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_USER_CREDENTIALS_ACCOUNT_ID",
                table: "USER_CREDENTIALS",
                column: "ACCOUNT_ID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_USER_PERMISSIONS_MODULE_ID",
                table: "USER_PERMISSIONS",
                column: "MODULE_ID");

            migrationBuilder.CreateIndex(
                name: "IX_USER_PERMISSIONS_USER_ID_MODULE_ID",
                table: "USER_PERMISSIONS",
                columns: new[] { "USER_ID", "MODULE_ID" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_USER_TOKENS_USER_ACCOUNTS_ACCOUNT_ID",
                table: "USER_TOKENS",
                column: "ACCOUNT_ID",
                principalTable: "USER_ACCOUNTS",
                principalColumn: "ACCOUNT_ID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
