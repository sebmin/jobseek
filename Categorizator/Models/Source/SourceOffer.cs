namespace Categorizator.Models.Source;

public sealed class SourceOffer
{
    public string? Guid { get; init; }
    public string? Slug { get; init; }
    public string? Title { get; init; }
    public string? DescriptionRaw { get; init; }
    public string? CompanyDescription { get; init; }
    public string? CompanyName { get; init; }
    public string? CompanySize { get; init; }
    public string? CountryCode { get; init; }
    public string? City { get; init; }
    public LabeledValue? ExperienceLevel { get; init; }
    public LabeledValue? WorkplaceType { get; init; }
    public LabeledValue? WorkingTime { get; init; }
    public CategoryValue? Category { get; init; }
    public HybridScheduleValue? HybridWorkSchedule { get; init; }
    public List<SourceEmployment>? EmploymentTypes { get; init; }
    public List<SourceSkill>? RequiredSkills { get; init; }
    public List<SourceSkill>? NiceToHaveSkills { get; init; }
    public List<SourceLanguage>? Languages { get; init; }
    public List<SourceLocation>? Multilocation { get; init; }
}
