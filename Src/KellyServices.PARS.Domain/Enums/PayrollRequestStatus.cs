namespace KellyServices.PARS.Domain.Enums
{
    public enum PayrollRequestStatus
    {
        Submitted,
        CandidateReview,
        DatabaseSearchRequired,
        DatabaseSearchInProgress,
        DocumentReview,
        FulfillmentQueued,
        Fulfilled,
        UnableToFulfill,
        Rejected
    }
}
