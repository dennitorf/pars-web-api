# Kelly PARS API

ASP.NET Core microservice for the Payroll Archive Retrieval System (PARS), created from the installed `asp-net-core-microservice` template. All entity primary and foreign keys use `Guid`.

## Included domain and API

The SQL-backed model contains employees, archive documents, ingestion batches, audit events, and email fulfillments. Entity Framework configurations add unique indexes for Kelly ID, source/checksum, and blob location.

- `GET /api/archive-documents` — search metadata by Kelly ID/name, document type, and year range.
- `GET /api/archive-documents/{documentId}/preview` — record the preview audit event.
- `GET /api/archive-documents/{documentId}/content` — stream the private blob through the authorized API.
- `POST /api/archive-documents/{documentId}/downloads` — record the download and return the API content route.
- `POST /api/archive-documents/{documentId}/email-fulfillments` — persist an audited fulfillment request.
- `GET /api/archive-operations/employees` — query document counts, storage size, and completeness by employee.
- `GET /api/archive-operations/audit-events` — query activity by actor, employee, document, event ID, action, and date.
- `GET /api/health` — template health endpoint.

## SFTP ingestion worker

The optional hosted worker polls for one CSV manifest, validates the whole manifest, downloads each referenced file to a temporary stream, verifies byte length and SHA-256, uploads it to a private Azure Blob container, and records the outcome. A successfully transferred SFTP file is moved to the processed directory. The manifest is moved only after every row succeeds.

The deterministic blob name is `year/document-type/kelly-id/sha256.ext`; reprocessing the same manifest is therefore idempotent. Invalid or oversized manifests remain on SFTP and are never partially processed. Run only one worker-enabled API replica unless a distributed lease is added.

Required CSV columns:

```csv
KellyId,EmployeeName,MaskedTaxId,DocumentType,DocumentYear,DocumentPeriod,RemoteFilePath,FileSizeBytes,Sha256
K100245,Jordan Walker,***-**-4821,W-2,2024,2024,/outbound/pars/files/K100245-W2-2024.pdf,104857,64-character-lowercase-sha256
```

Enable and configure with environment variables (double underscores represent nested configuration):

```text
ArchiveIngestion__Enabled=true
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
