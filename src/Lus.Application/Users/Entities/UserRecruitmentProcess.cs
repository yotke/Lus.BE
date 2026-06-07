using Lus.Application.Common;

namespace Lus.Application.Users.Entities
{
    public class UserRecruitmentProcess : EntityBase<int>
    {
        public int CurrentStep { get; set; }

        public string AppData { get; set; }

        public int ApplicationDateId { get; set; }

        public int UserId { get; set; }

        public int? CandidateId { get; set; }

        public User User { get; set; }

        public int RecruitmentProcessId { get; set; }

    }
}
