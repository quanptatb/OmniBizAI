using Microsoft.AspNetCore.Mvc.Rendering;

namespace OmniBizAI.Helpers;

/// <summary>
/// Centralized enum → SelectList converter with Vietnamese labels.
/// </summary>
public static class EnumHelper
{
    /// <summary>
    /// Get a list of SelectListItem from any enum, with Vietnamese display names.
    /// </summary>
    public static List<SelectListItem> GetSelectList<TEnum>(string? selectedValue = null, bool includeAll = false) where TEnum : struct, Enum
    {
        var items = new List<SelectListItem>();
        if (includeAll)
            items.Add(new SelectListItem { Value = "", Text = "Tất cả" });

        foreach (var val in Enum.GetValues<TEnum>())
        {
            var name = val.ToString();
            items.Add(new SelectListItem
            {
                Value = name,
                Text = EnumLabels.Get<TEnum>(val),
                Selected = name == selectedValue
            });
        }
        return items;
    }

    /// <summary>
    /// Get Vietnamese label for a single enum value.
    /// </summary>
    public static string Label<TEnum>(TEnum value) where TEnum : struct, Enum
        => EnumLabels.Get(value);

    /// <summary>
    /// Get Vietnamese label for enum value string.
    /// </summary>
    public static string Label<TEnum>(string? value) where TEnum : struct, Enum
    {
        if (string.IsNullOrEmpty(value)) return value ?? "";
        return Enum.TryParse<TEnum>(value, out var parsed) ? EnumLabels.Get(parsed) : value;
    }
}
