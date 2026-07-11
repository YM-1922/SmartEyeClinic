namespace SmartEyeClinic.Web.Services;

/// <summary>
/// نموذج يمثل نتيجة تنفيذ العمليات في الخدمات البرمجية (نجاح أو فشل مع رسالة توضيحية)
/// </summary>
public class ServiceResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    // إرجاع نتيجة نجاح العملية
    public static ServiceResult Ok() => new() { Success = true };

    // إرجاع نتيجة فشل العملية مع رسالة توضح السبب
    public static ServiceResult Fail(string message) => new() { Success = false, Message = message };
}
