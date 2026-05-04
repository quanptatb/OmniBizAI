using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OmniBizAI.ViewModels
{
    public enum KpiDirection { Higher, Lower }
    public enum KpiStatus { OnTrack, AtRisk, Behind, Completed }
    public enum CheckInStatus { Pending, Approved, Rejected }
    public enum UserRole { Staff, Manager }

    public class KeyResultViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public double StartValue { get; set; }
        public double CurrentValue { get; set; }
        public double TargetValue { get; set; }
        public string Unit { get; set; } = string.Empty;
        public KpiDirection Direction { get; set; } = KpiDirection.Higher;

        public double RawProgress
        {
            get
            {
                if (Direction == KpiDirection.Higher)
                    return TargetValue == StartValue ? 0 : (CurrentValue - StartValue) / (TargetValue - StartValue) * 100;
                else
                    return StartValue == TargetValue ? 0 : (StartValue - CurrentValue) / (StartValue - TargetValue) * 100;
            }
        }

        public double ClampedProgress => Math.Max(0, Math.Min(100, RawProgress));
        public bool IsOverTarget => RawProgress > 100;
    }

    public class KpiViewModel
    {
        public int Id { get; set; }
        public string Objective { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string OwnerAvatar { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public KpiStatus Status { get; set; }
        public List<KeyResultViewModel> KeyResults { get; set; } = [];

        public double OverallProgress
        {
            get
            {
                if (KeyResults.Count == 0) return 0;
                double sum = 0;
                foreach (var kr in KeyResults) sum += kr.ClampedProgress;
                return sum / KeyResults.Count;
            }
        }

        public string GradeLetter
        {
            get
            {
                var p = OverallProgress;
                return p >= 90 ? "A" : p >= 70 ? "B" : p >= 50 ? "C" : p >= 30 ? "D" : "E";
            }
        }

        public string GradeLabel
        {
            get
            {
                return GradeLetter switch
                {
                    "A" => "Xuất sắc",
                    "B" => "Tốt",
                    "C" => "Trung bình",
                    "D" => "Yếu",
                    _ => "Kém"
                };
            }
        }
    }

    public class CheckInViewModel
    {
        public int Id { get; set; }
        public int KpiId { get; set; }
        public int KeyResultId { get; set; }
        public string KpiObjective { get; set; } = string.Empty;
        public string KeyResultTitle { get; set; } = string.Empty;
        public double PreviousValue { get; set; }
        public double NewValue { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string SubmittedBy { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public CheckInStatus Status { get; set; }
        public string? ManagerNote { get; set; }
    }

    public class CheckInFormViewModel
    {
        public int KpiId { get; set; }
        public string KpiObjective { get; set; } = string.Empty;
        public List<KeyResultViewModel> KeyResults { get; set; } = [];

        [Required(ErrorMessage = "Vui lòng chọn Key Result")]
        public int SelectedKeyResultId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá trị mới")]
        public double NewValue { get; set; }

        [StringLength(1000)]
        public string? Note { get; set; }

        public string? EvidenceFileName { get; set; }
    }

    public class PendingCheckInViewModel
    {
        public List<CheckInViewModel> PendingItems { get; set; } = [];
    }
}
