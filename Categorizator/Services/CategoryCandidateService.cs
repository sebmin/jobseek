using Categorizator.Models.CandidateSource;
using Categorizator.Models.Contract;

namespace Categorizator.Services
{
    public class CategoryCandidateService
    {
        public CandidateDto CreateCategoryCandidateDto(Candidate candidate)
        {
            var candidateDto = new CandidateDto(candidate.Skills?.Select(s => new SkillDto(s.Name ?? "Unknown", s.Level)).ToList() ?? []);
            
            return candidateDto;
        }

        public static CategoryCandidateService CreateService() => new();//TODO: IoC
    }
}
