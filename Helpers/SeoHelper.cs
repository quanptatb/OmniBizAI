using Microsoft.AspNetCore.Mvc.Rendering;

namespace OmniBizAI.Helpers
{
    public static class SeoHelper
    {
        public static string GetTitle(ViewContext viewContext, string defaultTitle = "OmniBizAI")
        {
            var title = viewContext.ViewData["Title"] as string;
            return string.IsNullOrEmpty(title) ? defaultTitle : $"{title} - {defaultTitle}";
        }

        public static string GetDescription(ViewContext viewContext, string defaultDesc = "Hệ thống vận hành thông minh cho doanh nghiệp — quản lý KPI, OKR, quy trình và tài chính.")
        {
            return viewContext.ViewData["Description"] as string ?? defaultDesc;
        }

        public static string GetKeywords(ViewContext viewContext, string defaultKeywords = "KPI, OKR, quản lý vận hành, AI, OmniBizAI, quản trị doanh nghiệp")
        {
            return viewContext.ViewData["Keywords"] as string ?? defaultKeywords;
        }

        public static string GetCanonicalUrl(ViewContext viewContext, string baseUrl = "https://omnibiz.ai")
        {
            var path = viewContext.HttpContext.Request.Path;
            return $"{baseUrl}{path}";
        }
    }
}
