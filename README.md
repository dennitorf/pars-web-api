# Kelly PARS API

ASP.NET Core microservice for the Payroll Archive Retrieval System (PARS), created from the installed `asp-net-core-microservice` template. All entity primary and foreign keys use `Guid`.

## Included domain and API

The SQL-backed model contains employees, archive documents, ingestion batches, audit events, and email fulfillments. Entity Framework configurations add unique indexes for Kelly ID, source/checksum, and blob location.

## CQRS structure

The API follows the same template convention as `plants-api`: Web API controllers only bind HTTP input, call `IMediator.Send`, and shape the HTTP response. Business rules, persistence, auditing, storage access, and workflow transitions live under `KellyServices.PARS.Application/Features` in feature-specific `Commands`, `Queries`, handlers, validators, and DTOs. MediatR logging, validation, and performance behaviors run for every request.

```text
Application/Features/
  ArchiveDocuments/{Commands,Queries,Models}
  ArchiveOperations/{Queries,Models}
  ArchiveIngestion/Commands/RunArchiveIngestion
  PayrollRequests/{Commands,Queries,Models}
WebApi/
  Controllers/                 HTTP and Mediator only
  BackgroundServices/         cron trigger only
```

- `GET /api/archive-documents` — search metadata by Kelly ID/name, document type, and year range.
- `GET /api/archive-documents/{documentId}/preview` — record the preview audit event.
- `GET /api/archive-documents/{documentId}/content` — stream the private blob through the authorized API.
- `POST /api/archive-documents/{documentId}/downloads` — record the download and return the API content route.
- `POST /api/archive-documents/{documentId}/email-fulfillments` — persist an audited fulfillment request.
- `GET /api/archive-operations/employees` — query document counts, storage size, and completeness by employee.
- `GET /api/archive-operations/audit-events` — query activity by actor, employee, document, event ID, action, and date.
- `GET /api/archive-operations/ingestion-batches` — query ingestion history, counts, status, and errors.
- `POST /api/archive-ingestion/runs` — execute the ingestion command on demand.
- `POST /api/payroll-requests` — receive an employee request and run deterministic attribute matching.
- `GET /api/payroll-requests` and `GET /api/payroll-requests/{id}` — query the specialist queue and request detail.
- `POST /api/payroll-requests/{id}/database-search` — initiate the deeper employee-record search when deterministic matching fails.
- `POST /api/payroll-requests/{id}/candidates/{candidateId}/confirm` — record the specialist’s employee-record decision and load documents in the requested range.
- `POST /api/payroll-requests/{id}/documents/{documentId}/selection` — include or exclude a reviewed document.
- `POST /api/payroll-requests/{id}/fulfill` — create request-linked, audited email fulfillment records for selected documents.
- `GET /api/health` — template health endpoint.

## SFTP ingestion command

`RunArchiveIngestionCommand` owns the complete ingestion use case. It checks for one CSV manifest, validates the whole manifest, downloads each referenced file to a temporary stream, verifies byte length and SHA-256, uploads it to a private Azure Blob container, and records the outcome. A successfully transferred SFTP file is moved to the processed directory. The manifest is moved only after every row succeeds.

The command can be dispatched by the on-demand endpoint or by `ArchiveIngestionScheduler`. The scheduler contains no ingestion logic; it calculates the next UTC occurrence from a five-field cron expression and sends the same MediatR command. A process-level execution lock prevents scheduled and on-demand runs from overlapping within one API instance.

The deterministic blob name is `year/document-type/kelly-id/sha256.ext`; reprocessing the same manifest is therefore idempotent. Invalid or oversized manifests remain on SFTP and are never partially processed. In a multi-replica deployment, enable the scheduler on only one replica unless a distributed lease is added.

## Payroll request workflow

An employee request contains name, email, a date range, desired document types, and either Kelly ID or the last four tax-ID digits. PARS proposes employee archive candidates and records which attributes contributed to the confidence score; a payroll specialist must confirm the record before any documents can be selected. If no deterministic candidate exists, the request enters `DatabaseSearchRequired` and must use the explicit database-search action. Confirming a candidate loads only available documents for that employee, requested type, and year range. Fulfillment is blocked until the employee record is confirmed and at least one document has been reviewed and selected.

Request, candidate, document-selection, and fulfillment identifiers are `Guid`. Fulfillments retain a foreign key to the originating request, and material workflow transitions are written to the archive audit log. The tax-ID fragment is sensitive identity evidence: production SQL configuration must encrypt it at rest, restrict it to the request-processing role, and redact it from logs and responses.

Required CSV columns:

```csv
KellyId,EmployeeName,MaskedTaxId,DocumentType,DocumentYear,DocumentPeriod,RemoteFilePath,FileSizeBytes,Sha256
K100245,Jordan Walker,***-**-4821,W-2,2024,2024,/outbound/pars/files/K100245-W2-2024.pdf,104857,64-character-lowercase-sha256
```

Enable and configure with environment variables (double underscores represent nested configuration):

```text
ArchiveIngestion__Enabled=true
ArchiveIngestion__CronExpression=*/5 * * * *
ArchiveIngestion__MetadataRemotePath=/outbound/pars/archive-metadata.csv
ArchiveIngestion__ProcessedDirectory=/outbound/pars/processed
ArchiveIngestion__BlobContainer=payroll-archive
ArchiveSftp__Host=sftp.example.com
ArchiveSftp__Username=pars-ingestion
ArchiveSftp__PrivateKeyBase64=<base64-private-key>
ArchiveSftp__ExpectedHostKeySha256=<base64-sha256-host-key>
ArchiveStorage__ServiceUri=https://<account>.blob.core.windows.net
```

For Azure, leave `ArchiveStorage__ConnectionString` unset and grant the workload managed identity Blob Data Contributor on the archive container. `UseDevelopmentStorage=true` is checked in only as the local Azurite default. Password authentication is also supported through `ArchiveSftp__Password`, but the private key and SFTP host-key pin are preferred. Secrets must come from environment configuration or Key Vault, never source control.

## Production boundary

Before production use, configure Entra ID authentication and role policies for Payroll, Legal, Tax, and EFSC. Metadata remains masked and storage paths are not returned by search endpoints. Every preview, download, ingestion, and email request is audited. Use a hot or cool blob access tier for the retrieval SLA; Azure Archive tier is unsuitable for immediate document access because it requires rehydration.
