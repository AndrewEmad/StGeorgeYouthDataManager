using System.Text.RegularExpressions;
namespace YouthDataManager.Shared.Service.Abstractions;

public static class AcademicYears
{
    public static IReadOnlyList<string> Values { get; } = ["سنة اولى","سنة ثانية","سنة ثالثة","سنة رابعة","سنة خامسة","سنة سادسة","سنة سابعة","حديث التخرج","غير معروف"];
    private static readonly IReadOnlyDictionary<string,string> Aliases = new Dictionary<string,string>(StringComparer.Ordinal)
    {
        ["اولي"] = "سنة اولى",
        ["اول"] = "سنة اولى",
        ["الاولي"] = "سنة اولى",
        ["الاول"] = "سنة اولى",
        ["1"] = "سنة اولى",
        ["١"] = "سنة اولى",
        ["۱"] = "سنة اولى",
        ["ثانيه"] = "سنة ثانية",
        ["ثاني"] = "سنة ثانية",
        ["الثانيه"] = "سنة ثانية",
        ["الثاني"] = "سنة ثانية",
        ["تانيه"] = "سنة ثانية",
        ["تاني"] = "سنة ثانية",
        ["2"] = "سنة ثانية",
        ["٢"] = "سنة ثانية",
        ["۲"] = "سنة ثانية",
        ["ثالثه"] = "سنة ثالثة",
        ["ثالث"] = "سنة ثالثة",
        ["الثالثه"] = "سنة ثالثة",
        ["الثالث"] = "سنة ثالثة",
        ["تالته"] = "سنة ثالثة",
        ["تالت"] = "سنة ثالثة",
        ["3"] = "سنة ثالثة",
        ["٣"] = "سنة ثالثة",
        ["۳"] = "سنة ثالثة",
        ["رابعه"] = "سنة رابعة",
        ["رابع"] = "سنة رابعة",
        ["الرابعه"] = "سنة رابعة",
        ["الرابع"] = "سنة رابعة",
        ["4"] = "سنة رابعة",
        ["٤"] = "سنة رابعة",
        ["۴"] = "سنة رابعة",
        ["خامسه"] = "سنة خامسة",
        ["خامس"] = "سنة خامسة",
        ["الخامسه"] = "سنة خامسة",
        ["الخامس"] = "سنة خامسة",
        ["5"] = "سنة خامسة",
        ["٥"] = "سنة خامسة",
        ["۵"] = "سنة خامسة",
        ["سادسه"] = "سنة سادسة",
        ["سادس"] = "سنة سادسة",
        ["السادسه"] = "سنة سادسة",
        ["السادس"] = "سنة سادسة",
        ["6"] = "سنة سادسة",
        ["٦"] = "سنة سادسة",
        ["۶"] = "سنة سادسة",
        ["سابعه"] = "سنة سابعة",
        ["سابع"] = "سنة سابعة",
        ["السابعه"] = "سنة سابعة",
        ["السابع"] = "سنة سابعة",
        ["7"] = "سنة سابعة",
        ["٧"] = "سنة سابعة",
        ["۷"] = "سنة سابعة",
        ["غيرمعروف"] = "غير معروف",
        ["حديثيالتخرج"] = "حديث التخرج",
        ["حديثوالتخرج"] = "حديث التخرج",
        ["حديثاتالتخرج"] = "حديث التخرج",
        ["حديثالتخرج"] = "حديث التخرج",
        ["حديثتخرج"] = "حديث التخرج",
        ["حديثهالتخرج"] = "حديث التخرج",
        ["خريجحديث"] = "حديث التخرج",
        ["خريجهحديثه"] = "حديث التخرج",
        ["خريججديد"] = "حديث التخرج",
        ["خريجهجديده"] = "حديث التخرج",
    };
    public static string? Canonical(string? value)
    {
        if(string.IsNullOrWhiteSpace(value))return "";
        var key=Regex.Replace(value.ToLowerInvariant(),@"[\s\u200e\u200f\u061c\u200b\u064b-\u065f\u0670\u0640]","")
            .Replace('أ','ا').Replace('إ','ا').Replace('آ','ا').Replace('ٱ','ا').Replace('ى','ي').Replace('ی','ي').Replace('ة','ه');
        // Graduation notes describe the year, not a second academic stage.
        key=Regex.Replace(key,@"\((?:هيتخرج|سيتخرج|هتتخرج|ستتخرج).*$", "");
        foreach(var prefix in new[]{"السنهالدراسيه","سنهدراسيه","العامالدراسي","السنه","سنه","الفرقه","فرقه"})if(key.StartsWith(prefix,StringComparison.Ordinal)){key=key[prefix.Length..];break;}
        foreach(var suffix in new[]{"بالجامعه","جامعيه","جامعي","جامعه"})if(key.EndsWith(suffix,StringComparison.Ordinal)){key=key[..^suffix.Length];break;}
        if(Aliases.TryGetValue(key,out var canonical))return canonical;
        foreach(var prefix in new[]{"خريج","الخريج","اتخرجت","تخرجت","متخرج","المتخرج"})if(key.StartsWith(prefix,StringComparison.Ordinal))return "حديث التخرج";
        return null;
    }
}
