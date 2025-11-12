using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Responses
{
    public class TokenResponse
    {
        public string? AccessToken { get; init; }
        public string? RefreshToken { get; init; }
        public DateTime? AccessTokenExpiry { get; init; }
        public DateTime? RefreshTokenExpiry { get; init; }
    }
}
