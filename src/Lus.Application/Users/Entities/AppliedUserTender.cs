using Lus.Application.Common;

namespace Lus.Application.Users.Entities
{
    public class AppliedUserTender : EntityBase<int>
    {
        public int TenderId { get; set; }


        public int UserId { get; set; }

        public User User { get; set; }
    }
}
