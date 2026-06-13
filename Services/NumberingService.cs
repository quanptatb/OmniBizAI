using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities;

namespace OmniBizAI.Services;

public interface INumberingService
{
    Task<string> NextAsync(
        string sequenceKey,
        string prefix,
        int padLength = 4,
        int? year = null,
        CancellationToken cancellationToken = default);
}

public static class NumberingSequenceKeys
{
    public const string OperationRequest = "OP_REQ";
    public const string OperationPlan = "OP_PLAN";
    public const string Equipment = "EQUIP";
    public const string SparePart = "SP_PART";
    public const string Workspace = "WSPACE";
    public const string Incident = "INCIDENT";
    public const string PmSchedule = "PM_SCHED";
    public const string WorkOrder = "WORK_ORDER";
    public const string SparePartRequisition = "SP_REQ";
    public const string FailureMode = "FAIL_MODE";
}

public sealed class NumberingService : INumberingService
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;

    public NumberingService(ApplicationDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<string> NextAsync(
        string sequenceKey,
        string prefix,
        int padLength = 4,
        int? year = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sequenceKey))
            throw new ArgumentException("Sequence key is required.", nameof(sequenceKey));

        if (string.IsNullOrWhiteSpace(prefix))
            throw new ArgumentException("Prefix is required.", nameof(prefix));

        if (padLength < 1)
            throw new ArgumentOutOfRangeException(nameof(padLength), "Padding length must be greater than zero.");

        var normalizedKey = sequenceKey.Trim().ToUpperInvariant();
        var normalizedPrefix = prefix.Trim();
        var sequenceYear = year ?? 0;
        var now = DateTimeOffset.UtcNow;
        Guid? userId = _tenant.UserId == Guid.Empty ? null : _tenant.UserId;

        var ownsTransaction = _db.Database.CurrentTransaction is null;
        IDbContextTransaction? transaction = null;

        if (ownsTransaction)
        {
            transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        }

        try
        {
            var sequence = await _db.NumberSequences
                .FromSqlInterpolated($@"
                    SELECT TOP(1) *
                    FROM [NumberSequences] WITH (UPDLOCK, HOLDLOCK)
                    WHERE [TenantId] = {_tenant.TenantId}
                      AND [Code] = {normalizedKey}
                      AND [Year] = {sequenceYear}
                      AND [IsDeleted] = CAST(0 AS bit)")
                .SingleOrDefaultAsync(cancellationToken);

            if (sequence is null)
            {
                sequence = new NumberSequence
                {
                    TenantId = _tenant.TenantId,
                    Code = normalizedKey,
                    Prefix = normalizedPrefix,
                    CurrentNumber = 0,
                    PaddingLength = padLength,
                    Year = sequenceYear,
                    CreatedAt = now,
                    CreatedByUserId = userId
                };
                _db.NumberSequences.Add(sequence);
            }
            else
            {
                sequence.Prefix = normalizedPrefix;
                sequence.PaddingLength = padLength;
                sequence.UpdatedAt = now;
                sequence.UpdatedByUserId = userId;
            }

            sequence.CurrentNumber++;
            await _db.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return sequence.Prefix + sequence.CurrentNumber.ToString($"D{sequence.PaddingLength}", CultureInfo.InvariantCulture);
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }
}
