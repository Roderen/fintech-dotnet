using Domain.TopUps;
using Domain.Transactions;
using Domain.Users;
using Domain.Wallets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Application.Abstractions.Data;

public interface IApplicationDbContext
{
    DbSet<Wallet> Wallets { get; set; }
    DbSet<Transaction> Transactions { get; set; }
    DbSet<User> Users { get; set; } 
    DbSet<TopUp> TopUps { get; set; }
    DatabaseFacade Database { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}