using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OmniBizAI.ViewModels
{
    public class PaymentRequestViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tiêu đề yêu cầu")]
        public required string Title { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phòng ban")]
        public required string Department { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        public required string Category { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nhà cung cấp")]
        public required string Vendor { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn nguồn ngân sách")]
        public required string Budget { get; set; }

        public string? Description { get; set; }

        public List<LineItemViewModel> LineItems { get; set; } = [];
    }

    public class LineItemViewModel
    {
        public string? Description { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}