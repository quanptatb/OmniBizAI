using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Services;

public class MeetingSummaryImportService(
    ApplicationDbContext db,
    ITenantContext tenant,
    GeminiService gemini,
    OkrService okrService,
    KpiManagementService kpiService)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly Regex QuarterRegex = new(@"(?:q|quy|quý)\s*(?<quarter>[1-4])(?:\s*[-/ ]\s*(?<year>20\d{2}))?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex YearRegex = new(@"\b(20\d{2})\b", RegexOptions.Compiled);
    private static readonly Regex KrRefRegex = new(@"\bkr\s*(?<index>\d{1,2})\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TargetRegex = new(@"(?<value>\d+(?:[.,]\d+)?)\s*(?<unit>tỷ|ty|triệu|trieu|%|điểm|diem|khách hàng|khach hang|khách|khach|lead|cơ hội|co hoi|hợp đồng|hop dong|ticket|giờ|gio|ngày|ngay|lần|lan)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly string[] InverseCues = ["giảm", "giam", "xuống", "toi da", "tối đa", "không quá", "khong qua", "dưới", "duoi", "rút ngắn", "rut ngan"];
    private static readonly string[] StabilityCues = ["duy trì", "duy tri", "ổn định", "on dinh", "uptime", "nps", "csat", "sla"];

    public async Task<MeetingSummaryImportViewModel> GetFormAsync()
    {
        var vm = new MeetingSummaryImportViewModel
        {
            MeetingTitle = "Họp liên phòng ban chốt mục tiêu quý",
            DemoSummary = DemoMeetingSummary,
            AutoActivateKpis = true
        };

        return await PopulateLookupAsync(vm);
    }

    public async Task<MeetingSummaryImportViewModel> PopulateLookupAsync(MeetingSummaryImportViewModel vm)
    {
        var lookups = await BuildLookupsAsync();
        vm.Departments = lookups.Departments
            .Select(x => new SelectOption { Value = x.Id.ToString(), Text = x.Name })
            .ToList();
        vm.Missions = lookups.Missions
            .Select(x => new SelectOption { Value = x.Id.ToString(), Text = x.Name })
            .ToList();
        vm.Employees = lookups.Employees
            .Select(x => new SelectOption { Value = x.Id.ToString(), Text = x.Name })
            .ToList();
        vm.Periods = lookups.Periods
            .Select(x => new SelectOption { Value = x.Id.ToString(), Text = x.Name })
            .ToList();
        vm.DemoSummary = DemoMeetingSummary;
        return vm;
    }

    public async Task<MeetingSummaryImportViewModel> AnalyzeAsync(MeetingSummaryImportViewModel vm)
    {
        vm = await PopulateLookupAsync(vm);

        var lookups = await BuildLookupsAsync();
        var preview = await TryBuildAiPreviewAsync(vm.MeetingTitle, vm.SummaryContent, lookups)
            ?? BuildRuleBasedPreview(vm.MeetingTitle, vm.SummaryContent, lookups);

        vm.Preview = preview;
        vm.PreviewPayloadJson = JsonSerializer.Serialize(preview, JsonOptions);
        return vm;
    }

    public async Task<MeetingSummaryImportCommitResult> CommitAsync(MeetingSummaryImportCommitViewModel vm)
    {
        var preview = JsonSerializer.Deserialize<MeetingSummaryImportPreviewViewModel>(vm.PreviewPayloadJson, JsonOptions)
            ?? throw new InvalidOperationException("Không đọc được dữ liệu preview import.");

        if (string.IsNullOrWhiteSpace(preview.Okr.ObjectiveName))
            throw new InvalidOperationException("Preview chưa có objective hợp lệ để import.");

        if (!preview.Okr.KeyResults.Any())
            throw new InvalidOperationException("Preview chưa có key result hợp lệ để import.");

        var okrId = await okrService.CreateAsync(new OkrCreateViewModel
        {
            ObjectiveName = preview.Okr.ObjectiveName,
            Level = preview.Okr.Level,
            Cycle = preview.Okr.Cycle,
            SelectedDepartmentIds = preview.Okr.SelectedDepartmentIds,
            SelectedMissionIds = preview.Okr.SelectedMissionIds,
            SelectedEmployeeIds = preview.Okr.SelectedEmployeeIds,
            KeyResults = preview.Okr.KeyResults
                .Select(kr => new OkrKeyResultCreateItem
                {
                    KeyResultName = kr.KeyResultName,
                    Unit = kr.Unit,
                    TargetValue = kr.TargetValue,
                    IsInverse = kr.IsInverse
                })
                .ToList()
        });

        var createdObjective = await db.OkrObjectives
            .Include(o => o.KeyResults.Where(kr => !kr.IsDeleted))
            .FirstOrDefaultAsync(o => o.Id == okrId && o.TenantId == tenant.TenantId && !o.IsDeleted)
            ?? throw new InvalidOperationException("Không tìm thấy OKR vừa được tạo.");

        var createdKeyResults = createdObjective.KeyResults
            .ToDictionary(kr => NormalizeText(kr.KeyResultName), kr => kr.Id);

        var kpiIds = new List<Guid>();
        foreach (var importedKpi in preview.Kpis)
        {
            Guid? linkedKeyResultId = null;
            if (!string.IsNullOrWhiteSpace(importedKpi.LinkedKeyResultName))
            {
                createdKeyResults.TryGetValue(NormalizeText(importedKpi.LinkedKeyResultName), out var matchedKrId);
                linkedKeyResultId = matchedKrId == Guid.Empty ? null : matchedKrId;
            }

            var kpiId = await kpiService.CreateAsync(new KpiCreateViewModel
            {
                Name = importedKpi.Name,
                Description = importedKpi.Description,
                Unit = importedKpi.Unit,
                OwnerType = importedKpi.OwnerType,
                PeriodType = importedKpi.PeriodType,
                MeasureType = importedKpi.MeasureType,
                PropertyType = importedKpi.PropertyType,
                OrganizationUnitId = importedKpi.OrganizationUnitId,
                OkrObjectiveId = okrId,
                OkrKeyResultId = linkedKeyResultId,
                EvaluationPeriodId = importedKpi.EvaluationPeriodId,
                TargetValue = importedKpi.TargetValue,
                PassThreshold = importedKpi.PassThreshold,
                FailThreshold = importedKpi.FailThreshold,
                PeriodStart = importedKpi.PeriodStart,
                PeriodEnd = importedKpi.PeriodEnd,
                CheckInFrequencyDays = importedKpi.CheckInFrequencyDays,
                DeadlineTime = importedKpi.DeadlineTime,
                ReminderEnabled = importedKpi.ReminderEnabled
            });

            kpiIds.Add(kpiId);
        }

        if (vm.AutoActivateOkr)
            await okrService.ActivateAsync(okrId);

        if (vm.AutoActivateKpis)
        {
            foreach (var kpiId in kpiIds)
                await kpiService.ActivateAsync(kpiId);
        }

        var importJob = new ImportJob
        {
            TenantId = tenant.TenantId,
            UploadedByUserId = tenant.UserId,
            EntityName = "MeetingSummary-OkrKpi",
            FileName = $"{SanitizeFileName(preview.MeetingTitle)}.txt",
            StoragePath = "inline://meeting-summary",
            Status = ImportJobStatus.Committed,
            TotalRows = 1,
            SuccessRows = 1,
            ErrorRows = 0,
            CreatedByUserId = tenant.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.ImportJobs.Add(importJob);
        db.ImportStagingRows.Add(new ImportStagingRow
        {
            TenantId = tenant.TenantId,
            ImportJobId = importJob.Id,
            RowNumber = 1,
            RawDataJson = JsonSerializer.Serialize(new
            {
                preview.MeetingTitle,
                preview.SourceSummary
            }, JsonOptions),
            NormalizedDataJson = JsonSerializer.Serialize(preview, JsonOptions),
            IsValid = true,
            IsCommitted = true,
            CreatedByUserId = tenant.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync();

        return new MeetingSummaryImportCommitResult
        {
            OkrId = okrId,
            OkrName = preview.Okr.ObjectiveName,
            KpiIds = kpiIds,
            ImportJobId = importJob.Id,
            ParseMode = preview.ParseMode
        };
    }

    private async Task<MeetingSummaryImportPreviewViewModel?> TryBuildAiPreviewAsync(string meetingTitle, string summary, ImportLookups lookups)
    {
        if (!gemini.IsConfigured)
            return null;

        var response = await gemini.GenerateAsync(BuildAiSystemPrompt(), BuildAiUserPrompt(meetingTitle, summary, lookups), 0.2, 2400);
        if (!response.Success || string.IsNullOrWhiteSpace(response.Text))
            return null;

        var json = ExtractJson(response.Text);
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            var payload = JsonSerializer.Deserialize<AiMeetingImportPayload>(json, JsonOptions);
            if (payload?.Okr is null || string.IsNullOrWhiteSpace(payload.Okr.ObjectiveName))
                return null;

            var preview = new MeetingSummaryImportPreviewViewModel
            {
                MeetingTitle = meetingTitle,
                SourceSummary = summary,
                ParseMode = "Gemini AI",
                Narrative = payload.Narrative?.Trim() ?? string.Empty,
                Cycle = payload.Cycle?.Trim() ?? ExtractCycle(summary, lookups.Periods),
                Warnings = payload.Warnings?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().ToList() ?? new List<string>()
            };

            preview.Okr = MapAiOkr(payload.Okr, preview.Cycle, lookups);
            preview.Kpis = MapAiKpis(payload.Kpis, preview.Okr, lookups);

            EnsurePreviewCompleteness(preview, lookups);
            return preview;
        }
        catch
        {
            return null;
        }
    }

    private MeetingSummaryImportPreviewViewModel BuildRuleBasedPreview(string meetingTitle, string summary, ImportLookups lookups)
    {
        var lines = summary
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        var cycle = ExtractCycle(summary, lookups.Periods);
        var matchedDepartments = MatchEntities(summary, lookups.Departments);
        var matchedEmployees = MatchEntities(summary, lookups.Employees);
        var matchedMissions = MatchEntities(summary, lookups.Missions);

        var objective = ExtractObjective(lines, cycle, matchedDepartments.Select(x => x.Name).ToList());
        var keyResults = ParseKeyResults(lines, matchedDepartments.Select(x => x.Name).ToList());
        if (!keyResults.Any())
            keyResults = DeriveKeyResultsFromSummary(lines, matchedDepartments.Select(x => x.Name).ToList());

        var preview = new MeetingSummaryImportPreviewViewModel
        {
            MeetingTitle = meetingTitle,
            SourceSummary = summary,
            ParseMode = "RuleBased",
            Narrative = $"Hệ thống nhận diện {matchedDepartments.Count} phòng ban, {keyResults.Count} key result và đang dựng {Math.Max(1, keyResults.Count)} KPI từ biên bản cuộc họp.",
            Cycle = cycle,
            Okr = new ImportedOkrDraftViewModel
            {
                ObjectiveName = objective,
                Level = InferOkrLevel(summary, matchedDepartments.Count, matchedEmployees.Count),
                Cycle = cycle,
                SelectedDepartmentIds = matchedDepartments.Select(x => x.Id).ToList(),
                DepartmentNames = matchedDepartments.Select(x => x.Name).ToList(),
                SelectedMissionIds = matchedMissions.Select(x => x.Id).ToList(),
                MissionNames = matchedMissions.Select(x => x.Name).ToList(),
                SelectedEmployeeIds = matchedEmployees.Select(x => x.Id).ToList(),
                EmployeeNames = matchedEmployees.Select(x => x.Name).ToList(),
                KeyResults = keyResults
            }
        };

        preview.Kpis = ParseKpis(lines, preview.Okr, lookups);
        EnsurePreviewCompleteness(preview, lookups);
        return preview;
    }

    private void EnsurePreviewCompleteness(MeetingSummaryImportPreviewViewModel preview, ImportLookups lookups)
    {
        if (string.IsNullOrWhiteSpace(preview.Okr.ObjectiveName))
        {
            preview.Okr.ObjectiveName = $"Chuẩn hóa mục tiêu từ cuộc họp {preview.MeetingTitle}";
            preview.Warnings.Add("Hệ thống chưa đọc rõ objective nên đã tạo objective mặc định từ tên cuộc họp.");
        }

        if (!preview.Okr.KeyResults.Any())
        {
            preview.Okr.KeyResults.Add(new ImportedOkrKeyResultDraftViewModel
            {
                Index = 1,
                KeyResultName = "Hoàn tất triển khai mục tiêu đã thống nhất",
                Unit = "%",
                TargetValue = 100
            });
            preview.Warnings.Add("Summary chưa có KR rõ ràng nên hệ thống đã thêm 1 KR mặc định để bạn tiếp tục chỉnh sửa.");
        }

        if (!preview.Kpis.Any())
        {
            preview.Kpis = DeriveKpisFromKeyResults(preview.Okr, lookups, preview.Cycle);
            preview.Warnings.Add("Summary chưa nêu KPI tường minh nên hệ thống tự suy luận KPI theo từng key result.");
        }

        if (!preview.Okr.DepartmentNames.Any())
            preview.Warnings.Add("Chưa khớp phòng ban nào trong master data. Bạn vẫn có thể import rồi chỉnh lại sau.");

        if (!preview.Okr.MissionNames.Any())
            preview.Warnings.Add("Chưa khớp định hướng chiến lược nào trong danh mục Mission/Vision hiện tại.");
    }

    private ImportedOkrDraftViewModel MapAiOkr(AiOkrPayload okr, string cycle, ImportLookups lookups)
    {
        var departments = ResolveEntitiesByNames(okr.DepartmentNames, lookups.Departments);
        var missions = ResolveEntitiesByNames(okr.MissionNames, lookups.Missions);
        var employees = ResolveEntitiesByNames(okr.OwnerNames, lookups.Employees);

        return new ImportedOkrDraftViewModel
        {
            ObjectiveName = okr.ObjectiveName?.Trim() ?? string.Empty,
            Level = Enum.TryParse<OkrLevel>(okr.Level, true, out var level) ? level : OkrLevel.Department,
            Cycle = string.IsNullOrWhiteSpace(okr.Cycle) ? cycle : okr.Cycle.Trim(),
            SelectedDepartmentIds = departments.Select(x => x.Id).ToList(),
            DepartmentNames = departments.Select(x => x.Name).ToList(),
            SelectedMissionIds = missions.Select(x => x.Id).ToList(),
            MissionNames = missions.Select(x => x.Name).ToList(),
            SelectedEmployeeIds = employees.Select(x => x.Id).ToList(),
            EmployeeNames = employees.Select(x => x.Name).ToList(),
            KeyResults = okr.KeyResults?.Select((kr, index) => new ImportedOkrKeyResultDraftViewModel
            {
                Index = index + 1,
                KeyResultName = kr.Name?.Trim() ?? $"KR {index + 1}",
                Unit = NormalizeUnit(kr.Unit),
                TargetValue = kr.TargetValue > 0 ? kr.TargetValue : 100,
                IsInverse = kr.IsInverse,
                DepartmentName = ResolveSingleEntityByName(kr.DepartmentName, lookups.Departments)?.Name
            }).ToList() ?? new List<ImportedOkrKeyResultDraftViewModel>()
        };
    }

    private List<ImportedKpiDraftViewModel> MapAiKpis(List<AiKpiPayload>? kpis, ImportedOkrDraftViewModel okr, ImportLookups lookups)
    {
        if (kpis is null || !kpis.Any())
            return new List<ImportedKpiDraftViewModel>();

        return kpis.Select((item, index) =>
        {
            var linkedKr = ResolveLinkedKeyResult(item.LinkedKeyResultName, item.LinkedKeyResultIndex, okr.KeyResults);
            var department = ResolveSingleEntityByName(item.DepartmentName, lookups.Departments);
            var periodType = Enum.TryParse<KpiPeriodType>(item.PeriodType, true, out var parsedPeriodType)
                ? parsedPeriodType
                : InferPeriodType(item.Name, okr.Cycle);
            var dateWindow = ResolveDateWindow(okr.Cycle, periodType);
            var period = ResolvePeriod(item.PeriodName, okr.Cycle, dateWindow, lookups.Periods);
            var propertyType = Enum.TryParse<KpiPropertyType>(item.PropertyType, true, out var parsedProperty)
                ? parsedProperty
                : InferPropertyType($"{item.Name} {item.Description}");
            var unit = NormalizeUnit(item.Unit) ?? linkedKr?.Unit ?? "Điểm";
            var targetValue = item.TargetValue > 0 ? item.TargetValue : linkedKr?.TargetValue ?? 100;

            return new ImportedKpiDraftViewModel
            {
                Name = item.Name?.Trim() ?? BuildKpiNameFromKr(linkedKr?.KeyResultName ?? $"KPI {index + 1}"),
                Description = item.Description?.Trim(),
                Unit = unit,
                OwnerType = Enum.TryParse<KpiOwnerType>(item.OwnerType, true, out var parsedOwnerType) ? parsedOwnerType : (department is null ? KpiOwnerType.Company : KpiOwnerType.Department),
                PeriodType = periodType,
                MeasureType = Enum.TryParse<KpiMeasureType>(item.MeasureType, true, out var parsedMeasure) ? parsedMeasure : InferMeasureType(unit, item.Description),
                PropertyType = propertyType,
                OrganizationUnitId = department?.Id,
                OrganizationUnitName = department?.Name,
                EvaluationPeriodId = period?.Id,
                EvaluationPeriodName = period?.Name,
                TargetValue = targetValue,
                PassThreshold = item.PassThreshold ?? InferPassThreshold(targetValue, propertyType),
                FailThreshold = item.FailThreshold ?? InferFailThreshold(targetValue, propertyType),
                PeriodStart = dateWindow.Start,
                PeriodEnd = dateWindow.End,
                CheckInFrequencyDays = item.CheckInFrequencyDays ?? InferFrequencyDays(periodType),
                DeadlineTime = item.DeadlineTime,
                ReminderEnabled = item.ReminderEnabled ?? true,
                LinkedKeyResultName = linkedKr?.KeyResultName,
                LinkedKeyResultIndex = linkedKr?.Index,
                OwnerName = ResolveSingleEntityByName(item.OwnerName, lookups.Employees)?.Name
            };
        }).ToList();
    }

    private List<ImportedOkrKeyResultDraftViewModel> ParseKeyResults(IReadOnlyList<string> lines, IReadOnlyList<string> departmentNames)
    {
        var krLines = lines
            .Where(line =>
            {
                var normalized = NormalizeText(line);
                return normalized.StartsWith("kr") || normalized.StartsWith("key result") || normalized.StartsWith("ket qua then chot");
            })
            .ToList();

        return krLines
            .Select((line, index) => ParseKeyResultLine(line, index + 1, departmentNames))
            .Where(item => item is not null)
            .Cast<ImportedOkrKeyResultDraftViewModel>()
            .ToList();
    }

    private List<ImportedOkrKeyResultDraftViewModel> DeriveKeyResultsFromSummary(IReadOnlyList<string> lines, IReadOnlyList<string> departmentNames)
    {
        var bulletLines = lines
            .Where(line => line.StartsWith('-') || line.StartsWith('*') || line.StartsWith('•'))
            .Take(4)
            .ToList();

        var results = bulletLines
            .Select((line, index) => ParseKeyResultLine(line, index + 1, departmentNames))
            .Where(item => item is not null)
            .Cast<ImportedOkrKeyResultDraftViewModel>()
            .ToList();

        if (results.Any())
            return results;

        var quantitativeSentences = lines
            .Where(line => TargetRegex.IsMatch(NormalizeText(line)))
            .Take(4)
            .ToList();

        return quantitativeSentences
            .Select((line, index) => ParseKeyResultLine(line, index + 1, departmentNames))
            .Where(item => item is not null)
            .Cast<ImportedOkrKeyResultDraftViewModel>()
            .ToList();
    }

    private ImportedOkrKeyResultDraftViewModel? ParseKeyResultLine(string line, int index, IReadOnlyList<string> departmentNames)
    {
        var cleaned = CleanMetricLine(line, "kr");
        if (string.IsNullOrWhiteSpace(cleaned))
            return null;

        var target = ParseTarget(cleaned);
        var name = SimplifyMetricName(cleaned);
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return new ImportedOkrKeyResultDraftViewModel
        {
            Index = index,
            KeyResultName = name,
            Unit = target.Unit ?? InferUnitFromText(cleaned),
            TargetValue = target.Value > 0 ? target.Value : 100,
            IsInverse = LooksInverse(cleaned),
            DepartmentName = departmentNames.FirstOrDefault(dept => NormalizeText(cleaned).Contains(NormalizeText(dept), StringComparison.Ordinal))
        };
    }

    private List<ImportedKpiDraftViewModel> ParseKpis(IReadOnlyList<string> lines, ImportedOkrDraftViewModel okr, ImportLookups lookups)
    {
        var kpiLines = lines
            .Where(line =>
            {
                var normalized = NormalizeText(line);
                return normalized.StartsWith("kpi") || normalized.StartsWith("chi tieu");
            })
            .ToList();

        if (!kpiLines.Any())
            return DeriveKpisFromKeyResults(okr, lookups, okr.Cycle);

        return kpiLines.Select((line, index) =>
        {
            var cleaned = CleanMetricLine(line, "kpi");
            var linkedKr = ResolveLinkedKeyResult(cleaned, okr.KeyResults);
            var parsedTarget = ParseTarget(cleaned);
            var department = MatchSingleEntity(cleaned, lookups.Departments)
                ?? ResolveDepartmentFromKr(linkedKr, lookups.Departments);
            var periodType = InferPeriodType(cleaned, okr.Cycle);
            var dateWindow = ResolveDateWindow(okr.Cycle, periodType);
            var period = ResolvePeriod(null, okr.Cycle, dateWindow, lookups.Periods);
            var propertyType = InferPropertyType(cleaned);
            var unit = parsedTarget.Unit ?? linkedKr?.Unit ?? InferUnitFromText(cleaned) ?? "Điểm";
            var targetValue = parsedTarget.Value > 0 ? parsedTarget.Value : linkedKr?.TargetValue ?? 100;

            return new ImportedKpiDraftViewModel
            {
                Name = SimplifyMetricName(cleaned, fallback: BuildKpiNameFromKr(linkedKr?.KeyResultName ?? $"KPI {index + 1}")),
                Description = cleaned,
                Unit = unit,
                OwnerType = department is null ? KpiOwnerType.Company : KpiOwnerType.Department,
                PeriodType = periodType,
                MeasureType = InferMeasureType(unit, cleaned),
                PropertyType = propertyType,
                OrganizationUnitId = department?.Id,
                OrganizationUnitName = department?.Name,
                EvaluationPeriodId = period?.Id,
                EvaluationPeriodName = period?.Name,
                TargetValue = targetValue,
                PassThreshold = InferPassThreshold(targetValue, propertyType),
                FailThreshold = InferFailThreshold(targetValue, propertyType),
                PeriodStart = dateWindow.Start,
                PeriodEnd = dateWindow.End,
                CheckInFrequencyDays = ExtractFrequencyDays(cleaned) ?? InferFrequencyDays(periodType),
                DeadlineTime = new TimeOnly(17, 0),
                ReminderEnabled = true,
                LinkedKeyResultName = linkedKr?.KeyResultName,
                LinkedKeyResultIndex = linkedKr?.Index,
                OwnerName = MatchSingleEntity(cleaned, lookups.Employees)?.Name
            };
        }).ToList();
    }

    private List<ImportedKpiDraftViewModel> DeriveKpisFromKeyResults(ImportedOkrDraftViewModel okr, ImportLookups lookups, string cycle)
    {
        return okr.KeyResults.Select((kr, index) =>
        {
            var periodType = kr.Unit == "Điểm" ? KpiPeriodType.Quarterly : InferPeriodType(kr.KeyResultName, cycle);
            var propertyType = kr.IsInverse ? KpiPropertyType.Reduction : InferPropertyType(kr.KeyResultName);
            var dateWindow = ResolveDateWindow(cycle, periodType);
            var period = ResolvePeriod(null, cycle, dateWindow, lookups.Periods);
            var department = ResolveSingleEntityByName(kr.DepartmentName, lookups.Departments)
                ?? ResolveSingleEntityByName(okr.DepartmentNames.FirstOrDefault(), lookups.Departments);

            return new ImportedKpiDraftViewModel
            {
                Name = BuildKpiNameFromKr(kr.KeyResultName),
                Description = $"KPI được tự suy luận từ KR{kr.Index}: {kr.KeyResultName}",
                Unit = kr.Unit ?? "Điểm",
                OwnerType = department is null ? KpiOwnerType.Company : KpiOwnerType.Department,
                PeriodType = periodType,
                MeasureType = InferMeasureType(kr.Unit, kr.KeyResultName),
                PropertyType = propertyType,
                OrganizationUnitId = department?.Id,
                OrganizationUnitName = department?.Name,
                EvaluationPeriodId = period?.Id,
                EvaluationPeriodName = period?.Name,
                TargetValue = kr.TargetValue,
                PassThreshold = InferPassThreshold(kr.TargetValue, propertyType),
                FailThreshold = InferFailThreshold(kr.TargetValue, propertyType),
                PeriodStart = dateWindow.Start,
                PeriodEnd = dateWindow.End,
                CheckInFrequencyDays = InferFrequencyDays(periodType),
                DeadlineTime = new TimeOnly(17, 0),
                ReminderEnabled = true,
                LinkedKeyResultName = kr.KeyResultName,
                LinkedKeyResultIndex = kr.Index
            };
        }).ToList();
    }

    private async Task<ImportLookups> BuildLookupsAsync()
    {
        var tid = tenant.TenantId;

        var departments = await db.OrganizationUnits
            .AsNoTracking()
            .Where(x => x.TenantId == tid && x.IsActive && !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new LookupEntity(
                x.Id,
                x.Name,
                NormalizeText(x.Name),
                BuildDepartmentAliases(x.Code, x.Name)))
            .ToListAsync();

        var missions = await db.MissionVisions
            .AsNoTracking()
            .Where(x => x.TenantId == tid && x.IsActive && !x.IsDeleted)
            .OrderByDescending(x => x.TargetYear)
            .Select(x => new LookupEntity(
                x.Id,
                x.Content ?? string.Empty,
                NormalizeText(x.Content ?? string.Empty),
                BuildTextAliases(x.Content ?? string.Empty)))
            .ToListAsync();

        var employees = await db.AppUsers
            .AsNoTracking()
            .Where(x => x.TenantId == tid && x.Status == UserStatus.Active && !x.IsDeleted)
            .OrderBy(x => x.FullName)
            .Select(x => new LookupEntity(
                x.Id,
                x.FullName,
                NormalizeText(x.FullName),
                BuildTextAliases($"{x.FullName} {x.Email}")))
            .ToListAsync();

        var periods = await db.EvaluationPeriods
            .AsNoTracking()
            .Where(x => x.TenantId == tid && !x.IsDeleted)
            .OrderByDescending(x => x.StartDate)
            .Select(x => new PeriodLookup(
                x.Id,
                x.PeriodName,
                NormalizeText(x.PeriodName),
                x.StartDate,
                x.EndDate,
                x.Status))
            .ToListAsync();

        return new ImportLookups(departments, missions, employees, periods);
    }

    private static string BuildAiSystemPrompt()
        => """
Bạn là trợ lý PMO chuyên chuyển biên bản họp thành cấu hình OKR/KPI cho hệ thống ERP.
Hãy trả về DUY NHẤT JSON hợp lệ, không thêm markdown, không giải thích.

Schema:
{
  "narrative": "string",
  "cycle": "Q3-2026",
  "warnings": ["string"],
  "okr": {
    "objectiveName": "string",
    "level": "Company|Department|Individual",
    "cycle": "Q3-2026",
    "departmentNames": ["string"],
    "missionNames": ["string"],
    "ownerNames": ["string"],
    "keyResults": [
      { "name": "string", "unit": "string", "targetValue": 0, "isInverse": false, "departmentName": "string" }
    ]
  },
  "kpis": [
    {
      "name": "string",
      "description": "string",
      "unit": "string",
      "ownerType": "Company|Department|User",
      "periodType": "Monthly|Quarterly|Yearly|Custom",
      "measureType": "Quantitative|Qualitative|Behavioral",
      "propertyType": "Growth|Stability|Reduction",
      "departmentName": "string",
      "periodName": "string",
      "targetValue": 0,
      "passThreshold": 0,
      "failThreshold": 0,
      "checkInFrequencyDays": 7,
      "deadlineTime": "17:00",
      "reminderEnabled": true,
      "linkedKeyResultName": "string",
      "linkedKeyResultIndex": 1,
      "ownerName": "string"
    }
  ]
}

Quy tắc:
- Giữ nguyên tinh thần của biên bản, ưu tiên tiếng Việt.
- Chỉ tạo objective và KPI có thể đo lường được.
- Nếu dữ liệu chưa chắc chắn, thêm cảnh báo vào warnings.
- Không bịa tên phòng ban, period, mission hay nhân sự ngoài danh sách tham chiếu.
""";

    private static string BuildAiUserPrompt(string meetingTitle, string summary, ImportLookups lookups)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Tiêu đề cuộc họp: {meetingTitle}");
        sb.AppendLine("Biên bản tóm tắt:");
        sb.AppendLine(summary);
        sb.AppendLine();
        sb.AppendLine("Danh mục phòng ban đang có:");
        sb.AppendLine(string.Join(" | ", lookups.Departments.Select(x => x.Name)));
        sb.AppendLine("Danh mục Mission/Vision đang có:");
        sb.AppendLine(string.Join(" | ", lookups.Missions.Select(x => x.Name)));
        sb.AppendLine("Danh mục nhân sự đang có:");
        sb.AppendLine(string.Join(" | ", lookups.Employees.Select(x => x.Name)));
        sb.AppendLine("Danh mục kỳ đánh giá đang có:");
        sb.AppendLine(string.Join(" | ", lookups.Periods.Select(x => x.Name)));
        return sb.ToString();
    }

    private static string ExtractJson(string input)
    {
        var trimmed = input.Trim();
        if (trimmed.StartsWith('{') && trimmed.EndsWith('}'))
            return trimmed;

        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        return start >= 0 && end > start
            ? trimmed[start..(end + 1)]
            : string.Empty;
    }

    private static string ExtractObjective(IReadOnlyList<string> lines, string cycle, IReadOnlyList<string> departments)
    {
        var objectiveLine = lines.FirstOrDefault(line =>
        {
            var normalized = NormalizeText(line);
            return normalized.StartsWith("objective") || normalized.StartsWith("muc tieu");
        });

        if (!string.IsNullOrWhiteSpace(objectiveLine))
            return SimplifyMetricName(CleanMetricLine(objectiveLine, "objective"), fallback: $"Mục tiêu quý {cycle}");

        var strategicLine = lines.FirstOrDefault(line =>
        {
            var normalized = NormalizeText(line);
            return normalized.Contains("uu tien") || normalized.Contains("trong tam") || normalized.Contains("ket luan");
        });

        if (!string.IsNullOrWhiteSpace(strategicLine))
            return SimplifyMetricName(strategicLine, fallback: $"Mục tiêu quý {cycle}");

        var scopeText = departments.Any() ? $" cho {string.Join(", ", departments.Take(2))}" : string.Empty;
        return $"Chuẩn hóa các mục tiêu trọng tâm{scopeText} trong {cycle}";
    }

    private static OkrLevel InferOkrLevel(string text, int departmentCount, int employeeCount)
    {
        var normalized = NormalizeText(text);
        if (normalized.Contains("cap cong ty") || normalized.Contains("toan cong ty") || normalized.Contains("ban giam doc"))
            return OkrLevel.Company;
        if (employeeCount == 1 && departmentCount == 0)
            return OkrLevel.Individual;
        if (departmentCount > 1)
            return OkrLevel.Company;
        return departmentCount == 1 ? OkrLevel.Department : OkrLevel.Company;
    }

    private static string ExtractCycle(string text, IReadOnlyList<PeriodLookup> periods)
    {
        var normalized = NormalizeText(text);
        var quarterMatch = QuarterRegex.Match(normalized);
        if (quarterMatch.Success)
        {
            var quarter = quarterMatch.Groups["quarter"].Value;
            var year = quarterMatch.Groups["year"].Success
                ? quarterMatch.Groups["year"].Value
                : YearRegex.Match(normalized).Groups[1].Value;
            if (!string.IsNullOrWhiteSpace(year))
                return $"Q{quarter}-{year}";
        }

        var matchedPeriod = periods.FirstOrDefault(period => normalized.Contains(period.NormalizedName, StringComparison.Ordinal));
        if (matchedPeriod is not null)
        {
            if (matchedPeriod.Name.Contains("Q", StringComparison.OrdinalIgnoreCase))
                return matchedPeriod.Name.Replace("/", "-").Trim();
            return $"{matchedPeriod.StartDate:MM/yyyy} - {matchedPeriod.EndDate:MM/yyyy}";
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var defaultQuarter = ((today.Month - 1) / 3) + 1;
        return $"Q{defaultQuarter}-{today.Year}";
    }

    private static List<LookupEntity> MatchEntities(string source, IReadOnlyList<LookupEntity> candidates)
    {
        var normalized = NormalizeText(source);
        return candidates
            .Where(candidate => candidate.Aliases.Any(alias => normalized.Contains(alias, StringComparison.Ordinal)))
            .GroupBy(candidate => candidate.Id)
            .Select(group => group.First())
            .ToList();
    }

    private static LookupEntity? MatchSingleEntity(string source, IReadOnlyList<LookupEntity> candidates)
    {
        var normalized = NormalizeText(source);
        return candidates
            .Select(candidate => new
            {
                Candidate = candidate,
                Score = candidate.Aliases.Count(alias => normalized.Contains(alias, StringComparison.Ordinal))
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Candidate.Name.Length)
            .Select(x => x.Candidate)
            .FirstOrDefault();
    }

    private static List<LookupEntity> ResolveEntitiesByNames(IEnumerable<string>? names, IReadOnlyList<LookupEntity> candidates)
    {
        if (names is null)
            return new List<LookupEntity>();

        return names
            .Select(name => ResolveSingleEntityByName(name, candidates))
            .Where(entity => entity is not null)
            .Cast<LookupEntity>()
            .DistinctBy(entity => entity.Id)
            .ToList();
    }

    private static LookupEntity? ResolveSingleEntityByName(string? name, IReadOnlyList<LookupEntity> candidates)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var normalized = NormalizeText(name);
        return candidates.FirstOrDefault(candidate =>
            candidate.NormalizedName == normalized ||
            candidate.Aliases.Contains(normalized) ||
            normalized.Contains(candidate.NormalizedName, StringComparison.Ordinal) ||
            candidate.NormalizedName.Contains(normalized, StringComparison.Ordinal));
    }

    private static ImportedOkrKeyResultDraftViewModel? ResolveLinkedKeyResult(string? source, List<ImportedOkrKeyResultDraftViewModel> keyResults)
    {
        if (string.IsNullOrWhiteSpace(source))
            return null;

        var referencedIndex = ExtractKrIndexReference(source);
        return ResolveLinkedKeyResult(null, referencedIndex, keyResults)
            ?? keyResults.FirstOrDefault(kr => NormalizeText(source).Contains(NormalizeText(kr.KeyResultName), StringComparison.Ordinal));
    }

    private static ImportedOkrKeyResultDraftViewModel? ResolveLinkedKeyResult(string? name, int? index, List<ImportedOkrKeyResultDraftViewModel> keyResults)
    {
        if (index.HasValue)
            return keyResults.FirstOrDefault(kr => kr.Index == index.Value);

        if (string.IsNullOrWhiteSpace(name))
            return null;

        var normalized = NormalizeText(name);
        return keyResults.FirstOrDefault(kr =>
            NormalizeText(kr.KeyResultName) == normalized ||
            NormalizeText(kr.KeyResultName).Contains(normalized, StringComparison.Ordinal) ||
            normalized.Contains(NormalizeText(kr.KeyResultName), StringComparison.Ordinal));
    }

    private static int? ExtractKrIndexReference(string text)
    {
        var match = KrRefRegex.Match(NormalizeText(text));
        if (!match.Success)
            return null;

        return int.TryParse(match.Groups["index"].Value, out var index) ? index : null;
    }

    private static LookupEntity? ResolveDepartmentFromKr(ImportedOkrKeyResultDraftViewModel? linkedKr, IReadOnlyList<LookupEntity> departments)
        => linkedKr is null ? null : ResolveSingleEntityByName(linkedKr.DepartmentName, departments);

    private static PeriodLookup? ResolvePeriod(string? explicitPeriodName, string cycle, DateWindow window, IReadOnlyList<PeriodLookup> periods)
    {
        if (!string.IsNullOrWhiteSpace(explicitPeriodName))
        {
            var direct = periods.FirstOrDefault(period =>
                period.NormalizedName == NormalizeText(explicitPeriodName) ||
                period.NormalizedName.Contains(NormalizeText(explicitPeriodName), StringComparison.Ordinal));
            if (direct is not null)
                return direct;
        }

        var normalizedCycle = NormalizeText(cycle).Replace("-", "/");
        var byCycle = periods.FirstOrDefault(period =>
            period.NormalizedName.Contains(normalizedCycle, StringComparison.Ordinal) ||
            normalizedCycle.Contains(period.NormalizedName, StringComparison.Ordinal));
        if (byCycle is not null)
            return byCycle;

        return periods.FirstOrDefault(period => period.StartDate == window.Start && period.EndDate == window.End)
            ?? periods.FirstOrDefault(period => period.StartDate <= window.Start && period.EndDate >= window.End);
    }

    private static DateWindow ResolveDateWindow(string cycle, KpiPeriodType periodType)
    {
        var normalized = NormalizeText(cycle);
        var quarterMatch = QuarterRegex.Match(normalized);
        if (quarterMatch.Success && int.TryParse(quarterMatch.Groups["quarter"].Value, out var quarter))
        {
            var yearText = quarterMatch.Groups["year"].Success
                ? quarterMatch.Groups["year"].Value
                : YearRegex.Match(normalized).Groups[1].Value;

            if (int.TryParse(yearText, out var year))
            {
                var firstMonth = ((quarter - 1) * 3) + 1;
                var start = new DateOnly(year, firstMonth, 1);
                var end = new DateOnly(year, firstMonth + 2, DateTime.DaysInMonth(year, firstMonth + 2));
                return new DateWindow(start, end);
            }
        }

        var yearMatch = YearRegex.Match(normalized);
        if (yearMatch.Success && int.TryParse(yearMatch.Groups[1].Value, out var parsedYear))
            return new DateWindow(new DateOnly(parsedYear, 1, 1), new DateOnly(parsedYear, 12, 31));

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (periodType == KpiPeriodType.Yearly)
            return new DateWindow(new DateOnly(today.Year, 1, 1), new DateOnly(today.Year, 12, 31));

        var defaultQuarter = ((today.Month - 1) / 3) + 1;
        var defaultFirstMonth = ((defaultQuarter - 1) * 3) + 1;
        return new DateWindow(new DateOnly(today.Year, defaultFirstMonth, 1), new DateOnly(today.Year, defaultFirstMonth + 2, DateTime.DaysInMonth(today.Year, defaultFirstMonth + 2)));
    }

    private static KpiPeriodType InferPeriodType(string? text, string cycle)
    {
        var normalized = NormalizeText(text ?? string.Empty);
        if (normalized.Contains("theo nam", StringComparison.Ordinal) || normalized.Contains("hang nam", StringComparison.Ordinal) || normalized.Contains(" moi nam", StringComparison.Ordinal))
            return KpiPeriodType.Yearly;
        if (normalized.Contains("theo quy", StringComparison.Ordinal) || normalized.Contains("hang quy", StringComparison.Ordinal) || NormalizeText(cycle).Contains("q", StringComparison.Ordinal))
            return normalized.Contains("thang", StringComparison.Ordinal) ? KpiPeriodType.Monthly : KpiPeriodType.Quarterly;
        if (normalized.Contains("hang thang", StringComparison.Ordinal) || normalized.Contains("thang", StringComparison.Ordinal) || normalized.Contains("30 ngay", StringComparison.Ordinal))
            return KpiPeriodType.Monthly;
        return KpiPeriodType.Monthly;
    }

    private static KpiMeasureType InferMeasureType(string? unit, string? text)
    {
        var normalizedUnit = NormalizeText(unit ?? string.Empty);
        if (!string.IsNullOrWhiteSpace(normalizedUnit))
            return KpiMeasureType.Quantitative;

        var normalizedText = NormalizeText(text ?? string.Empty);
        if (normalizedText.Contains("hanh vi") || normalizedText.Contains("tuan thu"))
            return KpiMeasureType.Behavioral;
        return KpiMeasureType.Qualitative;
    }

    private static KpiPropertyType InferPropertyType(string? text)
    {
        var normalized = NormalizeText(text ?? string.Empty);
        if (InverseCues.Any(cue => normalized.Contains(cue, StringComparison.Ordinal)))
            return KpiPropertyType.Reduction;
        if (StabilityCues.Any(cue => normalized.Contains(cue, StringComparison.Ordinal)))
            return KpiPropertyType.Stability;
        return KpiPropertyType.Growth;
    }

    private static bool LooksInverse(string text)
    {
        var normalized = NormalizeText(text);
        return InverseCues.Any(cue => normalized.Contains(cue, StringComparison.Ordinal));
    }

    private static decimal InferPassThreshold(decimal targetValue, KpiPropertyType propertyType)
    {
        if (targetValue <= 0)
            return 0;

        return propertyType == KpiPropertyType.Reduction
            ? Math.Round(targetValue * 1.05m, 2)
            : Math.Round(targetValue * 0.90m, 2);
    }

    private static decimal InferFailThreshold(decimal targetValue, KpiPropertyType propertyType)
    {
        if (targetValue <= 0)
            return 0;

        return propertyType == KpiPropertyType.Reduction
            ? Math.Round(targetValue * 1.25m, 2)
            : Math.Round(targetValue * 0.75m, 2);
    }

    private static int InferFrequencyDays(KpiPeriodType periodType) => periodType switch
    {
        KpiPeriodType.Monthly => 7,
        KpiPeriodType.Quarterly => 14,
        KpiPeriodType.Yearly => 30,
        _ => 14
    };

    private static int? ExtractFrequencyDays(string text)
    {
        var normalized = NormalizeText(text);
        var weekly = Regex.Match(normalized, @"(?<days>\d{1,3})\s*ngay");
        if (weekly.Success && int.TryParse(weekly.Groups["days"].Value, out var days))
            return days;
        if (normalized.Contains("hang tuan") || normalized.Contains("7 ngay"))
            return 7;
        if (normalized.Contains("2 tuan") || normalized.Contains("14 ngay"))
            return 14;
        if (normalized.Contains("hang thang") || normalized.Contains("30 ngay"))
            return 30;
        return null;
    }

    private static ParsedTarget ParseTarget(string text)
    {
        var match = TargetRegex.Matches(NormalizeText(text)).LastOrDefault();
        if (match is null || !match.Success)
            return new ParsedTarget(0, null);

        if (!decimal.TryParse(match.Groups["value"].Value.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            return new ParsedTarget(0, NormalizeUnit(match.Groups["unit"].Value));

        return new ParsedTarget(value, NormalizeUnit(match.Groups["unit"].Value));
    }

    private static string? InferUnitFromText(string text)
    {
        var match = TargetRegex.Match(NormalizeText(text));
        return match.Success ? NormalizeUnit(match.Groups["unit"].Value) : null;
    }

    private static string? NormalizeUnit(string? unit)
    {
        if (string.IsNullOrWhiteSpace(unit))
            return null;

        return NormalizeText(unit) switch
        {
            "ty" or "tỷ" => "Tỷ VNĐ",
            "trieu" or "triệu" => "Triệu VNĐ",
            "diem" or "điểm" => "Điểm",
            "khach" or "khách" or "khach hang" or "khách hàng" => "Khách hàng",
            "co hoi" or "cơ hội" => "Cơ hội",
            "hop dong" or "hợp đồng" => "Hợp đồng",
            "gio" or "giờ" => "Giờ",
            "ngay" or "ngày" => "Ngày",
            "lan" or "lần" => "Lần",
            _ => unit.Trim().Replace("  ", " ")
        };
    }

    private static string CleanMetricLine(string line, string prefix)
    {
        var cleaned = line.Trim().TrimStart('-', '*', '•').Trim();
        cleaned = Regex.Replace(cleaned, $@"^{prefix}\s*\d*\s*[:\-]\s*", string.Empty, RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"^(ket qua then chot|kết quả then chốt)\s*\d*\s*[:\-]\s*", string.Empty, RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"^(objective|muc tieu|mục tiêu)\s*[:\-]\s*", string.Empty, RegexOptions.IgnoreCase);
        return cleaned.Trim();
    }

    private static string SimplifyMetricName(string text, string? fallback = null)
    {
        var candidate = text.Trim();
        var digitMatch = Regex.Match(candidate, @"\d");
        if (digitMatch.Success && digitMatch.Index > 0)
            candidate = candidate[..digitMatch.Index].Trim();

        candidate = Regex.Replace(candidate, @"^(phòng|phong)\s+.+?\s+theo dõi\s+kpi\s+", string.Empty, RegexOptions.IgnoreCase).Trim();
        candidate = Regex.Replace(candidate, @"^(bo phan|bộ phận)\s+.+?\s+theo dõi\s+kpi\s+", string.Empty, RegexOptions.IgnoreCase).Trim();
        candidate = Regex.Replace(candidate, @"\btheo dõi kpi\b", string.Empty, RegexOptions.IgnoreCase).Trim();
        candidate = Regex.Replace(candidate, @"\bvới mục tiêu\b.*$", string.Empty, RegexOptions.IgnoreCase).Trim();
        candidate = Regex.Replace(candidate, @"\bphòng chịu trách nhiệm\b.*$", string.Empty, RegexOptions.IgnoreCase).Trim();
        candidate = Regex.Replace(candidate, @"\bcheck[- ]?in\b.*$", string.Empty, RegexOptions.IgnoreCase).Trim();
        candidate = Regex.Replace(candidate, @"\bliên kết\b.*$", string.Empty, RegexOptions.IgnoreCase).Trim();
        candidate = Regex.Replace(candidate, @"\b(?:moi|mỗi)\s+(tháng|thang|quý|quy|năm|nam)\b.*$", string.Empty, RegexOptions.IgnoreCase).Trim();
        candidate = Regex.Replace(candidate, @"\bmỗi\s+(tháng|quý|năm)\b.*$", string.Empty, RegexOptions.IgnoreCase).Trim();
        candidate = candidate.Trim(' ', ':', '-', ',', ';', '.');

        return string.IsNullOrWhiteSpace(candidate) ? fallback ?? string.Empty : candidate;
    }

    private static string BuildKpiNameFromKr(string keyResultName)
    {
        var normalized = NormalizeText(keyResultName);
        if (normalized.Contains("doanh thu"))
            return "Doanh thu upsell khách hàng hiện hữu";
        if (normalized.Contains("chot") || normalized.Contains("co hoi"))
            return "Tỷ lệ chốt cơ hội upsell";
        if (normalized.Contains("go live") || normalized.Contains("trien khai"))
            return "Thời gian go-live trung bình";
        if (normalized.Contains("nps") || normalized.Contains("csat"))
            return "Điểm hài lòng sau triển khai";
        if (normalized.Contains("khach hang moi"))
            return "Số khách hàng doanh nghiệp mới";
        return $"KPI theo dõi {keyResultName}";
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "meeting-summary-import" : sanitized;
    }

    private static string NormalizeText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark)
                continue;

            builder.Append(char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch) ? ch : ' ');
        }

        return Regex.Replace(builder.ToString(), @"\s+", " ").Trim();
    }

    private static List<string> BuildDepartmentAliases(string? code, string name)
    {
        var aliases = BuildTextAliases($"{code} {name}");
        var normalized = NormalizeText(name);
        foreach (var prefix in new[] { "phong", "ban", "bo phan", "bo phận" })
        {
            if (normalized.StartsWith(prefix + " ", StringComparison.Ordinal))
                aliases.Add(normalized[(prefix.Length + 1)..].Trim());
        }
        return aliases.Distinct().Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
    }

    private static List<string> BuildTextAliases(string value)
    {
        var normalized = NormalizeText(value);
        var aliases = new List<string> { normalized };
        var pieces = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (pieces.Length >= 2)
            aliases.Add(string.Join(' ', pieces.Take(3)));
        return aliases.Distinct().Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
    }

    public const string DemoMeetingSummary =
        """
        Biên bản tóm tắt họp liên phòng ban ngày 02/07/2026 về kế hoạch chốt OKR/KPI quý 3/2026.

        Thành phần tham dự: Nguyễn Minh Tuấn (Giám đốc), Hoàng Thị Mai (Trưởng phòng Kinh doanh), Bùi Quang Hải (Trưởng phòng Marketing), Ngô Thị Thanh Hằng (Trưởng phòng Vận hành), Võ Thị Lan Anh (Trưởng phòng Tài chính).

        Kết luận chiến lược:
        - Ưu tiên bám mục tiêu chiến lược: Tăng doanh thu recurring từ khách hàng hiện hữu 25% trong năm 2026.
        - Marketing và Sales phải phối hợp tạo pipeline upsell cho nhóm khách hàng enterprise hiện hữu.
        - Operations cam kết rút ngắn thời gian go-live để không làm trễ ghi nhận doanh thu và giữ trải nghiệm khách hàng ở mức xuất sắc.

        Objective: Tăng doanh thu khách hàng hiện hữu thêm 18% trong Q3/2026 thông qua upsell gói dịch vụ ERP và rút ngắn thời gian triển khai.
        KR1: Doanh thu upsell từ nhóm khách hàng hiện hữu đạt 18 tỷ trong Q3/2026. Phòng chịu trách nhiệm: Phòng Kinh Doanh.
        KR2: Tỷ lệ chốt cơ hội upsell từ pipeline enterprise đạt 32%. Phòng chịu trách nhiệm: Phòng Marketing, Phòng Kinh Doanh.
        KR3: Thời gian go-live trung bình sau khi ký hợp đồng giảm xuống còn 21 ngày. Phòng chịu trách nhiệm: Phòng Vận Hành.
        KR4: Điểm NPS của khách hàng triển khai mới đạt tối thiểu 55 điểm trong quý. Phòng chịu trách nhiệm: Phòng Vận Hành.

        KPI1: Phòng Kinh Doanh theo dõi KPI Doanh thu upsell khách hàng hiện hữu với mục tiêu 6 tỷ mỗi tháng, check-in 7 ngày/lần, liên kết KR1.
        KPI2: Phòng Marketing theo dõi KPI Số demo upsell đủ điều kiện với mục tiêu 45 lead mỗi tháng, check-in 7 ngày/lần, liên kết KR2.
        KPI3: Phòng Vận Hành theo dõi KPI Thời gian go-live trung bình với mục tiêu tối đa 21 ngày, check-in 14 ngày/lần, liên kết KR3.
        KPI4: Phòng Vận Hành theo dõi KPI Điểm NPS sau triển khai với mục tiêu 55 điểm mỗi quý, check-in 30 ngày/lần, liên kết KR4.

        Yêu cầu đưa toàn bộ OKR/KPI lên hệ thống trước 05/07/2026 để Ban Giám đốc theo dõi trên dashboard.
        """;

    private sealed record LookupEntity(Guid Id, string Name, string NormalizedName, List<string> Aliases);
    private sealed record PeriodLookup(Guid Id, string Name, string NormalizedName, DateOnly StartDate, DateOnly EndDate, EvaluationPeriodStatus Status);
    private sealed record ImportLookups(
        List<LookupEntity> Departments,
        List<LookupEntity> Missions,
        List<LookupEntity> Employees,
        List<PeriodLookup> Periods);
    private sealed record ParsedTarget(decimal Value, string? Unit);
    private sealed record DateWindow(DateOnly Start, DateOnly End);

    private sealed class AiMeetingImportPayload
    {
        public string? Narrative { get; set; }
        public string? Cycle { get; set; }
        public List<string>? Warnings { get; set; }
        public AiOkrPayload? Okr { get; set; }
        public List<AiKpiPayload>? Kpis { get; set; }
    }

    private sealed class AiOkrPayload
    {
        public string? ObjectiveName { get; set; }
        public string? Level { get; set; }
        public string? Cycle { get; set; }
        public List<string>? DepartmentNames { get; set; }
        public List<string>? MissionNames { get; set; }
        public List<string>? OwnerNames { get; set; }
        public List<AiKeyResultPayload>? KeyResults { get; set; }
    }

    private sealed class AiKeyResultPayload
    {
        public string? Name { get; set; }
        public string? Unit { get; set; }
        public decimal TargetValue { get; set; }
        public bool IsInverse { get; set; }
        public string? DepartmentName { get; set; }
    }

    private sealed class AiKpiPayload
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Unit { get; set; }
        public string? OwnerType { get; set; }
        public string? PeriodType { get; set; }
        public string? MeasureType { get; set; }
        public string? PropertyType { get; set; }
        public string? DepartmentName { get; set; }
        public string? PeriodName { get; set; }
        public decimal TargetValue { get; set; }
        public decimal? PassThreshold { get; set; }
        public decimal? FailThreshold { get; set; }
        public int? CheckInFrequencyDays { get; set; }
        public TimeOnly? DeadlineTime { get; set; }
        public bool? ReminderEnabled { get; set; }
        public string? LinkedKeyResultName { get; set; }
        public int? LinkedKeyResultIndex { get; set; }
        public string? OwnerName { get; set; }
    }
}
