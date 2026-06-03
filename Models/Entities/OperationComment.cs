using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

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

    public OperationCommentType Type { get; set; } = OperationCommentType.Note;

    public Guid? ParentCommentId { get; set; }
    public OperationComment? ParentComment { get; set; }
    public ICollection<OperationComment> Replies { get; set; } = new List<OperationComment>();

    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;
}
