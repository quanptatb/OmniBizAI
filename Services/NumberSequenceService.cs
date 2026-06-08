using System.Data;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities;

namespace OmniBizAI.Services;

/// <summary>
/// Atomic tenant-scoped sequence generator. Replaces Count+1 pattern which has
/// a race condition under concurrent writes.
/// </summary>
public class NumberSequenceService
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;

    public NumberSequenceService(ApplicationDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<string> NextCodeAsync(string sequenceCode, string prefix, int padding = 4)
    {
        var tid = _tenant.TenantId;

        for (int attempt = 0; attempt < 5; attempt++)
        {
            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var seq = await _db.NumberSequences
                    .FirstOrDefaultAsync(s => s.TenantId == tid && s.Code == sequenceCode);

                if (seq == null)
                {
                    seq = new NumberSequence
                    {
                        TenantId = tid,
                        Code = sequenceCode,
                        Prefix = prefix,
                        CurrentNumber = 0,
                        PaddingLength = padding,
                        Year = DateTime.UtcNow.Year,
                        CreatedAt = DateTimeOffset.UtcNow,
                        CreatedByUserId = _tenant.UserId
                    };
                    _db.NumberSequences.Add(seq);
                }

                seq.CurrentNumber++;
                seq.UpdatedAt = DateTimeOffset.UtcNow;
                seq.UpdatedByUserId = _tenant.UserId;

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return $"{prefix}{seq.CurrentNumber.ToString().PadLeft(padding, '0')}";
            }
            catch (DbUpdateException) when (attempt < 4)
            {
                await tx.RollbackAsync();
                foreach (var entry in _db.ChangeTracker.Entries<NumberSequence>().ToList())
                    entry.State = EntityState.Detached;
                await Task.Delay(20 * (attempt + 1));
            }
        }

        throw new InvalidOperationException($"Không thể sinh mã cho sequence '{sequenceCode}' sau 5 lần thử.");
    }
}
