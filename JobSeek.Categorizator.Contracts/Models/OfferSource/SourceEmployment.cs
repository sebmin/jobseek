namespace JobSeek.Categorizator.Contracts.Models.OfferSource;

public sealed class SourceEmployment
{
    public string? Type { get; init; }
    public decimal? From { get; init; }
    public decimal? To { get; init; }
    public string? Currency { get; init; }
    public string? Unit { get; init; }
    public bool Gross { get; init; }
    public string? CurrencySource { get; init; }
}
