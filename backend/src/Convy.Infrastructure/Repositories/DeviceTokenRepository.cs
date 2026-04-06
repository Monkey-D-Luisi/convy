using Convy.Domain.Entities;
using Convy.Domain.Repositories;
using Convy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Convy.Infrastructure.Repositories;

public class DeviceTokenRepository : IDeviceTokenRepository
{
    private readonly ConvyDbContext _context;

    public DeviceTokenRepository(ConvyDbContext context)
    {
        _context = context;
    }

    public async Task<DeviceToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.DeviceTokens
            .FirstOrDefaultAsync(d => d.Token == token, cancellationToken);
    }

    public async Task<List<DeviceToken>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.DeviceTokens
            .Where(d => d.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DeviceToken>> GetByUserIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        return await _context.DeviceTokens
            .Where(d => userIds.Contains(d.UserId))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(DeviceToken deviceToken, CancellationToken cancellationToken = default)
    {
        await _context.DeviceTokens.AddAsync(deviceToken, cancellationToken);
    }

    public void Remove(DeviceToken deviceToken)
    {
        _context.DeviceTokens.Remove(deviceToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
