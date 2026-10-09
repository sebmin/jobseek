using JobSeek.Categorizator.Contracts.Models.CandidateSource;
using JobSeek.Categorizator.Contracts.Models.Contract;
using JobSeek.Categorizator.Contracts.Services;

namespace JobSeek.Categorizator.Services
{
    public class CategoryCandidateService : ICategoryCandidateService
    {
        public CandidateDto CreateCategoryCandidateDto(Candidate candidate)
        {
            var candidateDto = new CandidateDto(candidate.Skills?.Select(s => new SkillDto(s.Name ?? "Unknown", s.Level)).ToList() ?? []);
            
            return candidateDto;
        }

        public static CategoryCandidateService CreateService() => new();//TODO: IoC
    }
}
