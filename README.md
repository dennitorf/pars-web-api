# Kelly PARS API

Draft ASP.NET Core microservice created from the installed `asp-net-core-microservice` `dotnet new` template.

## Draft contract

- `GET /api/archive-documents` — query the curated metadata index by Kelly ID/name, document type, and year range.
- `GET /api/archive-documents/{documentId}/preview` — create a five-minute inline preview instruction and audit the view.
- `POST /api/archive-documents/{documentId}/downloads` — create a five-minute secure download instruction.
- `POST /api/archive-documents/{documentId}/email-fulfillments` — queue an audited employee email for review.
- `GET /api/archive-operations/employees` — query employee document counts and storage completeness.
- `GET /api/archive-operations/audit-events` — query activity by user, employee, document, event ID, action, and date range.
- `GET /api/health` — service health inherited from the microservice template.

The checked-in controller uses representative in-memory data so the contract and Swagger surface can be reviewed without connecting to Databricks or ADLS. Archive, download-request, and fulfillment identifiers use `Guid` values; domain entities inherit the same `Guid` primary-key convention. The production implementation should replace the sample repository with a Unity Catalog/Delta metadata repository, an ADLS signed-content service, Exchange or Logic Apps fulfillment, and an immutable audit sink.

## Security boundary

Before production use, configure Entra ID authentication and role policies for Payroll, Legal, Tax, and EFSC. Metadata responses must remain masked; raw storage paths must never be returned to callers. Every preview, download, and email fulfillment must record the caller, UTC timestamp, document ID, action, reason, and outcome.
