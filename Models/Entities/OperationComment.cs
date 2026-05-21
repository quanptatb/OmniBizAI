using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

/// <summary>
/// A comment left by a user on an Operation Request.
/// </summary>
public class OperationComment : TenantEntity
{
    public Guid OperationRequestId { get; set; }
    public OperationRequest? OperationRequest { get; set; }

    public Guid AuthorUserId { get; set; }
    public AppUser? AuthorUser { get; set; }

    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;
}
