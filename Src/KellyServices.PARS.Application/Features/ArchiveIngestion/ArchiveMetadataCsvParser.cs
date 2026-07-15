using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace KellyServices.PARS.Application.Features.ArchiveIngestion
{
    public class ArchiveMetadataCsvParser
    {
        private static readonly string[] RequiredHeaders =
        {
            "KellyId", "EmployeeName", "MaskedTaxId", "DocumentType", "DocumentYear",
            "DocumentPeriod", "RemoteFilePath", "FileSizeBytes", "Sha256"
        };

        public IReadOnlyList<ArchiveImportRecord> Parse(Stream csvStream)
        {
            csvStream.Position = 0;
            using var parser = new TextFieldParser(csvStream);
            parser.TextFieldType = FieldType.Delimited;
            parser.SetDelimiters(",");
            parser.HasFieldsEnclosedInQuotes = true;
            parser.TrimWhiteSpace = true;

            var headers = parser.ReadFields() ?? throw new InvalidDataException("The metadata CSV is empty.");
            var positions = headers.Select((header, index) => new { header, index })
                .ToDictionary(item => item.header, item => item.index, StringComparer.OrdinalIgnoreCase);

            var missing = RequiredHeaders.Where(header => !positions.ContainsKey(header)).ToArray();
            if (missing.Length > 0) throw new InvalidDataException($"Metadata CSV is missing required columns: {string.Join(", ", missing)}.");

            var records = new List<ArchiveImportRecord>();
            var line = 1;
            while (!parser.EndOfData)
            {
                line++;
                var fields = parser.ReadFields();
                if (fields is null || fields.All(string.IsNullOrWhiteSpace)) continue;

                string Value(string name) => positions[name] < fields.Length ? fields[positions[name]]?.Trim() : null;
                if (!int.TryParse(Value("DocumentYear"), NumberStyles.None, CultureInfo.InvariantCulture, out var year))
                    throw new InvalidDataException($"Invalid DocumentYear on CSV line {line}.");
                if (!long.TryParse(Value("FileSizeBytes"), NumberStyles.None, CultureInfo.InvariantCulture, out var size))
                    throw new InvalidDataException($"Invalid FileSizeBytes on CSV line {line}.");

                var record = new ArchiveImportRecord(Value("KellyId"), Value("EmployeeName"), Value("MaskedTaxId"), Value("DocumentType"), year,
                    Value("DocumentPeriod"), Value("RemoteFilePath"), size, Value("Sha256").ToLowerInvariant());

                if (string.IsNullOrWhiteSpace(record.KellyId) || string.IsNullOrWhiteSpace(record.RemoteFilePath) || string.IsNullOrWhiteSpace(record.Sha256))
                    throw new InvalidDataException($"KellyId, RemoteFilePath, and Sha256 are required on CSV line {line}.");
                if (record.Sha256.Length != 64 || !record.Sha256.All(Uri.IsHexDigit))
                    throw new InvalidDataException($"Sha256 must be a 64-character hexadecimal value on CSV line {line}.");

                records.Add(record);
            }

            return records;
        }
    }
}
