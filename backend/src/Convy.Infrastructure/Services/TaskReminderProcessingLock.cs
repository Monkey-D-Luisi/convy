using Convy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Convy.Infrastructure.Services;

public interface ITaskReminderProcessingLock
{
    Task<IAsyncDisposable?> TryAcquireAsync(CancellationToken cancellationToken);
}

public sealed class PostgresTaskReminderProcessingLock : ITaskReminderProcessingLock
{
    private const long LockKey = 987300210045L;

    private readonly ConvyDbContext _context;

    public PostgresTaskReminderProcessingLock(ConvyDbContext context)
    {
        _context = context;
    }

    public async Task<IAsyncDisposable?> TryAcquireAsync(CancellationToken cancellationToken)
    {
        await _context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = _context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "select pg_try_advisory_lock(@lock_key)";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "lock_key";
            parameter.Value = LockKey;
            command.Parameters.Add(parameter);

            var acquired = await command.ExecuteScalarAsync(cancellationToken);
            if (acquired is true)
                return new Releaser(_context, LockKey);

            await _context.Database.CloseConnectionAsync();
            return null;
        }
        catch
        {
            await _context.Database.CloseConnectionAsync();
            throw;
        }
    }

    private sealed class Releaser : IAsyncDisposable
    {
        private readonly ConvyDbContext _context;
        private readonly long _lockKey;

        public Releaser(ConvyDbContext context, long lockKey)
        {
            _context = context;
            _lockKey = lockKey;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await using var command = _context.Database.GetDbConnection().CreateCommand();
                command.CommandText = "select pg_advisory_unlock(@lock_key)";
                var parameter = command.CreateParameter();
                parameter.ParameterName = "lock_key";
                parameter.Value = _lockKey;
                command.Parameters.Add(parameter);
                await command.ExecuteScalarAsync();
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }
        }
    }
}
