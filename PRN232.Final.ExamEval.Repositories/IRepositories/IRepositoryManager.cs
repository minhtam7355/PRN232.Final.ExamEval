using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Repositories.IRepositories
{

    public interface IRepositoryManager
    {
        Task SaveAsync(CancellationToken ct = default);
        Task<IDbTransaction> BeginTransaction(CancellationToken ct = default);
    }
}
