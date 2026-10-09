using JobSeek.Categorizator.Contracts.Models.CandidateSource;
using JobSeek.Categorizator.Contracts.Models.Contract;

namespace JobSeek.Categorizator.Contracts.Services;

public interface ICategoryCandidateService
{
    CandidateDto CreateCategoryCandidateDto(Candidate candidate);
}
