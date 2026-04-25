using System;
using System.Collections.Generic;

namespace OmniBizAI.Models.Entities;

public partial class PaymentRequestAttachment
{
    public Guid Id { get; set; }

    public Guid PaymentRequestId { get; set; }

    public string FileName { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public long FileSize { get; set; }

    public string ContentType { get; set; } = null!;

    public Guid? UploadedBy { get; set; }

    public DateTime UploadedAt { get; set; }

    public virtual PaymentRequest PaymentRequest { get; set; } = null!;
}
