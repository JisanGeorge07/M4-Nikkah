using Application.Models.Transactions;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Persistence
{
    public interface ITransactionRepository
    {
        Task AddPaymentResultAsync(Transaction _transaction);
        Task<Transaction> GetTransactionByTxnIdAsync(string txnId);
        Task<List<Transaction>> GetAllTransactionsAsync();
        Task<List<Transaction>> GetTransactionsBasedOnDateAsync(DateTime? fromDate, DateTime? toDate);
        Task<List<Transaction>> GetTransactionsBasedOnNameDateAsync(DateTime? fromDate, DateTime? toDate, string name, string paymentType);
        Task<Transaction> GetTransactionByUserIdAsync(long userId);
        Task<List<Transaction>> GetPagedTransactionsByUserIdAsync(long userId, int page, int pageSize);
        Task<Transaction> GetTransactionByIdAsync(long id);
    }
}
