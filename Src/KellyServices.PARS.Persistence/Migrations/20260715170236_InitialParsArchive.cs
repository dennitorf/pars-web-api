using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KellyServices.PARS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialParsArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArchiveIngestionBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MetadataFilePath = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    MetadataChecksum = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RecordsDiscovered = table.Column<int>(type: "int", nullable: false),
                    RecordsTransferred = table.Column<int>(type: "int", nullable: false),
                    RecordsSkipped = table.Column<int>(type: "int", nullable: false),
                    RecordsFailed = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchiveIngestionBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeArchives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KellyId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EmployeeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MaskedTaxId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    StorageStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    StatusDetail = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeArchives", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ArchiveDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeArchiveId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IngestionBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DocumentYear = table.Column<int>(type: "int", nullable: false),
                    DocumentPeriod = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BlobContainer = table.Column<string>(type: "nvarchar(63)", maxLength: 63, nullable: false),
                    BlobName = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    SourcePath = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    SourceChecksum = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StoredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchiveDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArchiveDocuments_ArchiveIngestionBatches_IngestionBatchId",
                        column: x => x.IngestionBatchId,
                        principalTable: "ArchiveIngestionBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ArchiveDocuments_EmployeeArchives_EmployeeArchiveId",
                        column: x => x.EmployeeArchiveId,
                        principalTable: "EmployeeArchives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ArchiveAuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ActorId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ActorDisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EmployeeArchiveId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ArchiveDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchiveAuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArchiveAuditEvents_ArchiveDocuments_ArchiveDocumentId",
                        column: x => x.ArchiveDocumentId,
                        principalTable: "ArchiveDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ArchiveAuditEvents_EmployeeArchives_EmployeeArchiveId",
                        column: x => x.EmployeeArchiveId,
                        principalTable: "EmployeeArchives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ArchiveFulfillments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArchiveDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    RequestedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BusinessReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchiveFulfillments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArchiveFulfillments_ArchiveDocuments_ArchiveDocumentId",
                        column: x => x.ArchiveDocumentId,
                        principalTable: "ArchiveDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveAuditEvents_ActorId_OccurredAt",
                table: "ArchiveAuditEvents",
                columns: new[] { "ActorId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveAuditEvents_ArchiveDocumentId",
                table: "ArchiveAuditEvents",
                column: "ArchiveDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveAuditEvents_EmployeeArchiveId_OccurredAt",
                table: "ArchiveAuditEvents",
                columns: new[] { "EmployeeArchiveId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveAuditEvents_OccurredAt",
                table: "ArchiveAuditEvents",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveDocuments_BlobContainer_BlobName",
                table: "ArchiveDocuments",
                columns: new[] { "BlobContainer", "BlobName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveDocuments_EmployeeArchiveId_DocumentYear_DocumentType",
                table: "ArchiveDocuments",
                columns: new[] { "EmployeeArchiveId", "DocumentYear", "DocumentType" });

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveDocuments_IngestionBatchId",
                table: "ArchiveDocuments",
                column: "IngestionBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveDocuments_SourcePath_SourceChecksum",
                table: "ArchiveDocuments",
                columns: new[] { "SourcePath", "SourceChecksum" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveFulfillments_ArchiveDocumentId_RequestedAt",
                table: "ArchiveFulfillments",
                columns: new[] { "ArchiveDocumentId", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveFulfillments_Status_RequestedAt",
                table: "ArchiveFulfillments",
                columns: new[] { "Status", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveIngestionBatches_MetadataFilePath_MetadataChecksum",
                table: "ArchiveIngestionBatches",
                columns: new[] { "MetadataFilePath", "MetadataChecksum" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveIngestionBatches_StartedAt",
                table: "ArchiveIngestionBatches",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeArchives_KellyId",
                table: "EmployeeArchives",
                column: "KellyId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArchiveAuditEvents");

            migrationBuilder.DropTable(
                name: "ArchiveFulfillments");

            migrationBuilder.DropTable(
                name: "ArchiveDocuments");

            migrationBuilder.DropTable(
                name: "ArchiveIngestionBatches");

            migrationBuilder.DropTable(
                name: "EmployeeArchives");
        }
    }
}
