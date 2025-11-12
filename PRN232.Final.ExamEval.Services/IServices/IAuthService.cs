using PRN232.Final.ExamEval.Repositories.Entities;
using PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Requests;
using PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Services.IServices
{
    public interface IAuthService
    {
        Task<User?> ValidateUser(UserForAuthenticationRequest userForAuth, CancellationToken ct = default);
        Task<TokenResponse> CreateToken(User user, bool setRefreshExpiry, CancellationToken ct = default);
        Task<TokenResponse> RefreshToken(string accessToken, string refreshToken, CancellationToken ct = default);
        Task<UserForReturnResponse> GetUserByToken(string jwt, CancellationToken ct = default);
    }
}
