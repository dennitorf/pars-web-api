using KellyServices.PARS.Application.Features.ArchiveDocuments.Commands.CreateEmailFulfillment;
using KellyServices.PARS.Application.Features.PayrollRequests.Commands.SubmitPayrollRequest;

namespace KellyServices.PARS.Application.UnitTests;

[TestFixture]
public class ValidationTests
{
    [Test]
    public async Task SubmitRequest_ValidPayload_PassesValidation()
    {
        var command = new SubmitPayrollRequestCommand { FirstName = "Kelly", LastName = "Worker", Email = "kelly@example.com", KellyId = "K100", FromDate = new(2021, 1, 1), ToDate = new(2024, 12, 31) };
        Assert.That((await new SubmitPayrollRequestCommandValidator().ValidateAsync(command)).IsValid, Is.True);
    }

    [TestCase(null, null)]
    [TestCase("", "")]
    public async Task SubmitRequest_WithoutEmployeeIdentifier_FailsValidation(string kellyId, string lastFour)
    {
        var command = new SubmitPayrollRequestCommand { FirstName = "Kelly", LastName = "Worker", Email = "kelly@example.com", KellyId = kellyId, TaxIdLastFour = lastFour, FromDate = DateTime.Today, ToDate = DateTime.Today };
        Assert.That((await new SubmitPayrollRequestCommandValidator().ValidateAsync(command)).IsValid, Is.False);
    }

    [Test]
    public async Task SubmitRequest_InvalidDatesEmailAndTaxId_FailsValidation()
    {
        var command = new SubmitPayrollRequestCommand { FirstName = "Kelly", LastName = "Worker", Email = "bad", TaxIdLastFour = "12A", FromDate = new(2025, 1, 1), ToDate = new(2024, 1, 1) };
        var result = await new SubmitPayrollRequestCommandValidator().ValidateAsync(command);
        Assert.That(result.Errors, Has.Count.GreaterThanOrEqualTo(3));
    }

    [Test]
    public async Task EmailFulfillment_RequiresDocumentEmailAndReason()
    {
        var result = await new CreateEmailFulfillmentCommandValidator().ValidateAsync(new CreateEmailFulfillmentCommand());
        Assert.That(result.Errors.Select(x => x.PropertyName), Is.EquivalentTo(new[] { "DocumentId", "EmployeeEmail", "BusinessReason" }));
    }
}
