using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Repositories.Entities
{
    public class User : IdentityUser<Guid>
    {
        public bool IsActive { get; set; } = true;
        public DateTime JoinedDate { get; set; } = DateTime.UtcNow;

        // Refresh token management
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }

        //Navigation Properties
        public virtual UserProfile UserProfile { get; set; } = null!;

    }
}
