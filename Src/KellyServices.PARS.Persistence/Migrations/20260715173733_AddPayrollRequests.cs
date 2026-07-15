using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KellyServices.PARS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PayrollDataRequestId",
                table: "ArchiveFulfillments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PayrollDataRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EmployeeFirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EmployeeLastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EmployeeEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    KellyId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    TaxIdLastFour = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    FromDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ToDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestedDocumentTypes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AssignedTo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SpecialistNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SearchInitiatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ConfirmedEmployeeArchiveId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollDataRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollDataRequests_EmployeeArchives_ConfirmedEmployeeArchiveId",
                        column: x => x.ConfirmedEmployeeArchiveId,
                        principalTable: "EmployeeArchives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollRequestCandidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayrollDataRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeArchiveId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    MatchedAttributes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ReviewedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollRequestCandidates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollRequestCandidates_EmployeeArchives_EmployeeArchiveId",
                        column: x => x.EmployeeArchiveId,
                        principalTable: "EmployeeArchives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollRequestCandidates_PayrollDataRequests_PayrollDataRequestId",
                        column: x => x.PayrollDataRequestId,
                        principalTable: "PayrollDataRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayrollRequestDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayrollDataRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArchiveDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsSelected = table.Column<bool>(type: "bit", nullable: false),
                    ReviewedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollRequestDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollRequestDocuments_ArchiveDocuments_ArchiveDocumentId",
                        column: x => x.ArchiveDocumentId,
                        principalTable: "ArchiveDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollRequestDocuments_PayrollDataRequests_PayrollDataRequestId",
                        column: x => x.PayrollDataRequestId,
                        principalTable: "PayrollDataRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveFulfillments_PayrollDataRequestId",
                table: "ArchiveFulfillments",
                column: "PayrollDataRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollDataRequests_ConfirmedEmployeeArchiveId",
                table: "PayrollDataRequests",
                column: "ConfirmedEmployeeArchiveId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollDataRequests_EmployeeEmail_SubmittedAt",
                table: "PayrollDataRequests",
                columns: new[] { "EmployeeEmail", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollDataRequests_RequestNumber",
                table: "PayrollDataRequests",
                column: "RequestNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollDataRequests_Status_SubmittedAt",
                table: "PayrollDataRequests",
                columns: new[] { "Status", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRequestCandidates_EmployeeArchiveId",
                table: "PayrollRequestCandidates",
                column: "EmployeeArchiveId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRequestCandidates_PayrollDataRequestId_EmployeeArchiveId",
                table: "PayrollRequestCandidates",
                columns: new[] { "PayrollDataRequestId", "EmployeeArchiveId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRequestDocuments_ArchiveDocumentId",
                table: "PayrollRequestDocuments",
                column: "ArchiveDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRequestDocuments_PayrollDataRequestId_ArchiveDocumentId",
                table: "PayrollRequestDocuments",
                columns: new[] { "PayrollDataRequestId", "ArchiveDocumentId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ArchiveFulfillments_PayrollDataRequests_PayrollDataRequestId",
                table: "ArchiveFulfillments",
                column: "PayrollDataRequestId",
                principalTable: "PayrollDataRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ArchiveFulfillments_PayrollDataRequests_PayrollDataRequestId",
                table: "ArchiveFulfillments");

            migrationBuilder.DropTable(
                name: "PayrollRequestCandidates");

            migrationBuilder.DropTable(
                name: "PayrollRequestDocuments");

            migrationBuilder.DropTable(
                name: "PayrollDataRequests");

            migrationBuilder.DropIndex(
                name: "IX_ArchiveFulfillments_PayrollDataRequestId",
                table: "ArchiveFulfillments");

            migrationBuilder.DropColumn(
                name: "PayrollDataRequestId",
                table: "ArchiveFulfillments");
        }
    }
}
