using System.ComponentModel.DataAnnotations;

namespace OmniBizAI.ViewModels
{
    // ── Select option helper ────────────────────────────────────────────────────
    public record SelectOption(string Value, string Label);

    // ── Filter form (POST body) ─────────────────────────────────────────────────
    public class ReportFilterViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Vui lòng chọn loại báo cáo")]
        public string? ReportType { get; set; }
        [Required(ErrorMessage = "Vui lòng chọn kỳ báo cáo")]
        public string? Period { get; set; }
        public string? Department { get; set; }
        public string? DateFrom { get; set; }
        public string? DateTo { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.IsNullOrEmpty(DateFrom) && !string.IsNullOrEmpty(DateTo))
            {
                if (DateTime.TryParse(DateFrom, out var dFrom) && DateTime.TryParse(DateTo, out var dTo))
                {
                    if (dFrom > dTo)
                    {
                        yield return new ValidationResult("Từ ngày phải nhỏ hơn hoặc bằng Đến ngày", new[] { nameof(DateFrom), nameof(DateTo) });
                    }
                }
                else
                {
                    yield return new ValidationResult("Định dạng ngày không hợp lệ", new[] { nameof(DateFrom), nameof(DateTo) });
                }
            }
        }
    }

    // ── Page view-model ─────────────────────────────────────────────────────────
    public class ReportIndexViewModel
    {
        public string CurrentRole { get; set; } = "Staff";
        public bool CanExport { get; set; }

        public List<SelectOption> ReportTypes { get; set; } = [];
        public List<SelectOption> Periods { get; set; } = [];
        public List<SelectOption> Departments { get; set; } = [];
    }

    // ── Preview payload ─────────────────────────────────────────────────────────
    public class ReportPreviewData
    {
        public string Title { get; set; } = string.Empty;
        public List<SummaryCard> Summary { get; set; } = [];
        public List<ReportRow> Rows { get; set; } = [];
        public string AiSummary { get; set; } = string.Empty;
        public string[] ChartLabels { get; set; } = [];
        public int[] ChartValues { get; set; } = [];
    }

    public record SummaryCard(
        string Label,
        string Value,
        string Change,
        string Direction,   // "up" | "down" | "flat"
        string Color        // "green" | "blue" | "amber" | "red" | "pink"
    );

    public class ReportRow
    {
        public int Stt { get; set; }
        public string MaSo { get; set; } = string.Empty;
        public string TenMuc { get; set; } = string.Empty;
        public string GiaTri { get; set; } = string.Empty;
        public string TrangThai { get; set; } = string.Empty;
        public string NgayTao { get; set; } = string.Empty;
    }
}
