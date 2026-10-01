using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentPaymentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IS_PAID",
                table: "APPOINTMENTS",
                type: "NUMBER(10)",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PAID_AT",
                table: "APPOINTMENTS",
                type: "TIMESTAMP",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MEDICATION_ORDERS",
                columns: table => new
                {
                    MEDICATION_ORDER_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CLIENT_PET_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    VETERINARIAN_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    APPOINTMENT_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    IS_IN_HOUSE = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    REFERRED_TO = table.Column<string>(type: "VARCHAR2(200)", maxLength: 200, nullable: true),
                    REFERRAL_REASON = table.Column<string>(type: "VARCHAR2(500)", maxLength: 500, nullable: true),
                    STATUS = table.Column<string>(type: "VARCHAR2(30)", maxLength: 30, nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MEDICATION_ORDERS", x => x.MEDICATION_ORDER_ID);
                    table.ForeignKey(
                        name: "FK_MEDICATION_ORDERS_APPOINTMENTS_APPOINTMENT_ID",
                        column: x => x.APPOINTMENT_ID,
                        principalTable: "APPOINTMENTS",
                        principalColumn: "APPOINTMENT_ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MEDICATION_ORDERS_CLIENTS_PETS_CLIENT_PET_ID",
                        column: x => x.CLIENT_PET_ID,
                        principalTable: "CLIENTS_PETS",
                        principalColumn: "CLIENT_PET_ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MEDICATION_ORDERS_VETERINARIANS_VETERINARIAN_ID",
                        column: x => x.VETERINARIAN_ID,
                        principalTable: "VETERINARIANS",
                        principalColumn: "VETERINARIAN_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MEDICATIONS",
                columns: table => new
                {
                    MEDICATION_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    NAME = table.Column<string>(type: "VARCHAR2(150)", maxLength: 150, nullable: false),
                    CODE = table.Column<string>(type: "VARCHAR2(50)", maxLength: 50, nullable: true),
                    IS_ACTIVE = table.Column<int>(type: "NUMBER(10)", nullable: false, defaultValue: 1),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MEDICATIONS", x => x.MEDICATION_ID);
                });

            migrationBuilder.CreateTable(
                name: "PROCEDURE_ORDERS",
                columns: table => new
                {
                    PROCEDURE_ORDER_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    CLIENT_PET_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    VETERINARIAN_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    APPOINTMENT_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    IS_IN_HOUSE = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    REFERRED_TO = table.Column<string>(type: "VARCHAR2(200)", maxLength: 200, nullable: true),
                    REFERRAL_REASON = table.Column<string>(type: "VARCHAR2(500)", maxLength: 500, nullable: true),
                    STATUS = table.Column<string>(type: "VARCHAR2(30)", maxLength: 30, nullable: false),
                    RESULT_FILE_URL = table.Column<string>(type: "VARCHAR2(1000)", maxLength: 1000, nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PROCEDURE_ORDERS", x => x.PROCEDURE_ORDER_ID);
                    table.ForeignKey(
                        name: "FK_PROCEDURE_ORDERS_APPOINTMENTS_APPOINTMENT_ID",
                        column: x => x.APPOINTMENT_ID,
                        principalTable: "APPOINTMENTS",
                        principalColumn: "APPOINTMENT_ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PROCEDURE_ORDERS_CLIENTS_PETS_CLIENT_PET_ID",
                        column: x => x.CLIENT_PET_ID,
                        principalTable: "CLIENTS_PETS",
                        principalColumn: "CLIENT_PET_ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PROCEDURE_ORDERS_VETERINARIANS_VETERINARIAN_ID",
                        column: x => x.VETERINARIAN_ID,
                        principalTable: "VETERINARIANS",
                        principalColumn: "VETERINARIAN_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PROCEDURES",
                columns: table => new
                {
                    PROCEDURE_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    NAME = table.Column<string>(type: "VARCHAR2(150)", maxLength: 150, nullable: false),
                    CODE = table.Column<string>(type: "VARCHAR2(50)", maxLength: 50, nullable: true),
                    IS_ACTIVE = table.Column<int>(type: "NUMBER(10)", nullable: false, defaultValue: 1),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PROCEDURES", x => x.PROCEDURE_ID);
                });

            migrationBuilder.CreateTable(
                name: "SUPPLIES",
                columns: table => new
                {
                    SUPPLY_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    NAME = table.Column<string>(type: "VARCHAR2(150)", maxLength: 150, nullable: false),
                    UNIT = table.Column<string>(type: "VARCHAR2(50)", maxLength: 50, nullable: false),
                    UNIT_PRICE = table.Column<decimal>(type: "NUMBER", nullable: false),
                    STOCK = table.Column<decimal>(type: "NUMBER", nullable: false),
                    IS_ACTIVE = table.Column<int>(type: "NUMBER(10)", nullable: false, defaultValue: 1),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SUPPLIES", x => x.SUPPLY_ID);
                });

            migrationBuilder.CreateTable(
                name: "MEDICATION_ORDER_ITEMS",
                columns: table => new
                {
                    MEDICATION_ORDER_ITEM_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    MEDICATION_ORDER_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    MEDICATION_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    NOTES = table.Column<string>(type: "VARCHAR2(500)", maxLength: 500, nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MEDICATION_ORDER_ITEMS", x => x.MEDICATION_ORDER_ITEM_ID);
                    table.ForeignKey(
                        name: "FK_MEDICATION_ORDER_ITEMS_MEDICATIONS_MEDICATION_ID",
                        column: x => x.MEDICATION_ID,
                        principalTable: "MEDICATIONS",
                        principalColumn: "MEDICATION_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MEDICATION_ORDER_ITEMS_MEDICATION_ORDERS_MEDICATION_ORDER_ID",
                        column: x => x.MEDICATION_ORDER_ID,
                        principalTable: "MEDICATION_ORDERS",
                        principalColumn: "MEDICATION_ORDER_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PROCEDURE_ORDER_ITEMS",
                columns: table => new
                {
                    PROCEDURE_ORDER_ITEM_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    PROCEDURE_ORDER_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    PROCEDURE_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    NOTES = table.Column<string>(type: "VARCHAR2(500)", maxLength: 500, nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PROCEDURE_ORDER_ITEMS", x => x.PROCEDURE_ORDER_ITEM_ID);
                    table.ForeignKey(
                        name: "FK_PROCEDURE_ORDER_ITEMS_PROCEDURES_PROCEDURE_ID",
                        column: x => x.PROCEDURE_ID,
                        principalTable: "PROCEDURES",
                        principalColumn: "PROCEDURE_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PROCEDURE_ORDER_ITEMS_PROCEDURE_ORDERS_PROCEDURE_ORDER_ID",
                        column: x => x.PROCEDURE_ORDER_ID,
                        principalTable: "PROCEDURE_ORDERS",
                        principalColumn: "PROCEDURE_ORDER_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SUPPLY_CONSUMPTIONS",
                columns: table => new
                {
                    SUPPLY_CONSUMPTION_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    HOSPITALIZATION_STAY_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    SUPPLY_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    QUANTITY = table.Column<decimal>(type: "NUMBER", nullable: false),
                    UNIT_PRICE = table.Column<decimal>(type: "NUMBER", nullable: false),
                    TOTAL = table.Column<decimal>(type: "NUMBER", nullable: false),
                    REGISTERED_BY_USER_ID = table.Column<string>(type: "VARCHAR2(36)", nullable: false),
                    NOTES = table.Column<string>(type: "VARCHAR2(500)", maxLength: 500, nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SUPPLY_CONSUMPTIONS", x => x.SUPPLY_CONSUMPTION_ID);
                    table.ForeignKey(
                        name: "FK_SUPPLY_CONSUMPTIONS_STAY",
                        column: x => x.HOSPITALIZATION_STAY_ID,
                        principalTable: "HOSPITALIZATION_STAYS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SUPPLY_CONSUMPTIONS_SUPPLY",
                        column: x => x.SUPPLY_ID,
                        principalTable: "SUPPLIES",
                        principalColumn: "SUPPLY_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MEDICATION_ORDER_ITEMS_MEDICATION_ID",
                table: "MEDICATION_ORDER_ITEMS",
                column: "MEDICATION_ID");

            migrationBuilder.CreateIndex(
                name: "IX_MEDICATION_ORDER_ITEMS_MEDICATION_ORDER_ID",
                table: "MEDICATION_ORDER_ITEMS",
                column: "MEDICATION_ORDER_ID");

            migrationBuilder.CreateIndex(
                name: "IX_MEDICATION_ORDERS_APPOINTMENT_ID",
                table: "MEDICATION_ORDERS",
                column: "APPOINTMENT_ID");

            migrationBuilder.CreateIndex(
                name: "IX_MEDICATION_ORDERS_CLIENT_PET_ID",
                table: "MEDICATION_ORDERS",
                column: "CLIENT_PET_ID");

            migrationBuilder.CreateIndex(
                name: "IX_MEDICATION_ORDERS_VETERINARIAN_ID",
                table: "MEDICATION_ORDERS",
                column: "VETERINARIAN_ID");

            migrationBuilder.CreateIndex(
                name: "IX_PROCEDURE_ORDER_ITEMS_PROCEDURE_ID",
                table: "PROCEDURE_ORDER_ITEMS",
                column: "PROCEDURE_ID");

            migrationBuilder.CreateIndex(
                name: "IX_PROCEDURE_ORDER_ITEMS_PROCEDURE_ORDER_ID",
                table: "PROCEDURE_ORDER_ITEMS",
                column: "PROCEDURE_ORDER_ID");

            migrationBuilder.CreateIndex(
                name: "IX_PROCEDURE_ORDERS_APPOINTMENT_ID",
                table: "PROCEDURE_ORDERS",
                column: "APPOINTMENT_ID");

            migrationBuilder.CreateIndex(
                name: "IX_PROCEDURE_ORDERS_CLIENT_PET_ID",
                table: "PROCEDURE_ORDERS",
                column: "CLIENT_PET_ID");

            migrationBuilder.CreateIndex(
                name: "IX_PROCEDURE_ORDERS_VETERINARIAN_ID",
                table: "PROCEDURE_ORDERS",
                column: "VETERINARIAN_ID");

            migrationBuilder.CreateIndex(
                name: "IX_SUPPLY_CONSUMPTIONS_HOSPITALIZATION_STAY_ID",
                table: "SUPPLY_CONSUMPTIONS",
                column: "HOSPITALIZATION_STAY_ID");

            migrationBuilder.CreateIndex(
                name: "IX_SUPPLY_CONSUMPTIONS_SUPPLY_ID",
                table: "SUPPLY_CONSUMPTIONS",
                column: "SUPPLY_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MEDICATION_ORDER_ITEMS");

            migrationBuilder.DropTable(
                name: "PROCEDURE_ORDER_ITEMS");

            migrationBuilder.DropTable(
                name: "SUPPLY_CONSUMPTIONS");

            migrationBuilder.DropTable(
                name: "MEDICATIONS");

            migrationBuilder.DropTable(
                name: "MEDICATION_ORDERS");

            migrationBuilder.DropTable(
                name: "PROCEDURES");

            migrationBuilder.DropTable(
                name: "PROCEDURE_ORDERS");

            migrationBuilder.DropTable(
                name: "SUPPLIES");

            migrationBuilder.DropColumn(
                name: "IS_PAID",
                table: "APPOINTMENTS");

            migrationBuilder.DropColumn(
                name: "PAID_AT",
                table: "APPOINTMENTS");
        }
    }
}
