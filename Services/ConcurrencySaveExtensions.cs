using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;

namespace OmniBizAI.Services;

internal static class ConcurrencySaveExtensions
{
    public const string StaleRecordMessage = "Bản ghi đã được người khác cập nhật, vui lòng tải lại.";

    public static async Task<bool> SaveChangesWithConcurrencyAsync(this ApplicationDbContext db)
    {
        try
        {
            await db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    public static async Task<(bool Success, string Message)> SaveChangesWithConcurrencyMessageAsync(
        this ApplicationDbContext db,
        string successMessage)
    {
        try
        {
            await db.SaveChangesAsync();
            return (true, successMessage);
        }
        catch (DbUpdateConcurrencyException)
        {
            return (false, StaleRecordMessage);
        }
    }
}
