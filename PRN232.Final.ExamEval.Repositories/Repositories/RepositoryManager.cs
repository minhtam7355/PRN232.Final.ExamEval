using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PRN232.Final.ExamEval.Repositories.IRepositories;
using PRN232.Final.ExamEval.Repositories.Persistence;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Repositories.Repositories
{
    public sealed class RepositoryManager : IRepositoryManager
    {
        private readonly AppDbContext appDbContext;

        public RepositoryManager(AppDbContext context)
        {
            appDbContext = context;
        }

        public async Task<IDbTransaction> BeginTransaction(CancellationToken ct = default)
        {
            var transaction = await appDbContext.Database.BeginTransactionAsync(ct);
            return transaction.GetDbTransaction();
        }

        public Task SaveAsync(CancellationToken ct = default) => appDbContext.SaveChangesAsync(ct);

    }
}
