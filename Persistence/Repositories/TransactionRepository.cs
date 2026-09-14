using Application.Interfaces.Persistence;
using Application.Models.Transactions;
using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistence.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly AppDbContext _dbContext;
        public TransactionRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task AddPaymentResultAsync(Transaction _transaction)
        {

            _dbContext.Transaction.AddAsync(_transaction);
                await _dbContext.SaveChangesAsync();
        }
        public async Task<Transaction> GetTransactionByTxnIdAsync(string txnId)
        {
            if (string.IsNullOrWhiteSpace(txnId))
                return null;

            var cleanTxnId = txnId.Trim();
            return await _dbContext.Transaction
                .FirstOrDefaultAsync(t => t.TxnId != null && t.TxnId.ToLower() == cleanTxnId.ToLower());
        }
        public async Task<bool> IsTransactionIdExistsAsync(string txnId)
        {
            if (string.IsNullOrWhiteSpace(txnId))
                return false;

            var cleanTxnId = txnId.Trim();
            return await _dbContext.Transaction
                .AnyAsync(t => t.TxnId != null && t.TxnId.ToLower() == cleanTxnId.ToLower());
        }
        public async Task<List<Transaction>> GetAllTransactionsAsync()
        {
            return await _dbContext.Transaction.ToListAsync();
        }

        public async Task<Transaction> GetTransactionByUserIdAsync(long userId)
        {
            return await _dbContext.Transaction
                .FirstOrDefaultAsync(t => t.userId == userId);
        }

        public async Task<List<Transaction>> GetTransactionsBasedOnDateAsync(DateTime? fromDate, DateTime? toDate)
            {
                var query = _dbContext.Transaction.AsQueryable();

                if (fromDate.HasValue)
                {
                    query = query.Where(t => t.CreatedOn.Date >= fromDate.Value.Date);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(t => t.CreatedOn.Date <= toDate.Value.Date);
                }

            return await query.OrderBy(t => t.CreatedOn).ToListAsync();
        }
        public async Task<List<Transaction>> GetTransactionsBasedOnNameDateAsync(DateTime? fromDate, DateTime? toDate, string name, string paymentType)
        {
            var query = _dbContext.Transaction.AsQueryable();
            
            if (fromDate.HasValue)
            {
                query = query.Where(t => t.CreatedOn.Date >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(t => t.CreatedOn.Date <= toDate.Value.Date);
            }
            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(t => t.FirstName.Contains(name, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrEmpty(paymentType))
            {
                query = query.Where(t => t.PaymentType == paymentType);
            }

            return await query.OrderByDescending(t => t.CreatedOn).ToListAsync();
        }

        public async Task<List<Transaction>> GetPagedTransactionsByUserIdAsync(long userId, int page, int pageSize)
        {
            return await _dbContext.Transaction
                .Where(t => t.userId == userId)
                .OrderByDescending(t => t.CreatedOn)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Transaction> GetTransactionByIdAsync(long id)
        {
            return await _dbContext.Transaction
                .Include(t => t.Registration)
                .FirstOrDefaultAsync(t => t.Id == id);
        }
    }
}
