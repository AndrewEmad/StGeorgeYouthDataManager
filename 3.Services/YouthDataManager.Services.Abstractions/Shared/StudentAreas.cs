using System.Text.RegularExpressions;
namespace YouthDataManager.Shared.Service.Abstractions;

public static class StudentAreas
{
    public static IReadOnlyList<string> Values { get; } = Array.AsReadOnly(new[] {"ابراهيم بك","ابن الحكم","ارض نوبار","الاسكان الصناعي","التعاون","الفرنواني","القناطر الخيرية","الكورنيش","المظلات","الملف","المنشية","المنشية الجديدة","الوحدة","ام بيومي","باسوس","بهتيم","جامع ابو الهنا","شبرا البلد","شبرا مصر","عبود","عزبة عثمان","عزبه رستم","قهوة شرف","كلية الزراعة","كوبرى عرابى","مدينة السلام","مساكن الصباغة","مساكن الضباط","مساكن حجازى","مسطرد","منطي","اخرى"});
    private static readonly IReadOnlyDictionary<string,string> Aliases=BuildAliases();
    private static IReadOnlyDictionary<string,string> BuildAliases()
    {
        var aliases=Values.ToDictionary(Key,value=>value,StringComparer.Ordinal);
        foreach(var (alias,canonical) in new (string Alias,string Canonical)[]
        {
            ("شبرا المحطة","شبرا البلد"),
            ("شبرا الخيمة","شبرا البلد"),
            ("شبرا الخيمه","شبرا البلد"),
            ("ارض نوبار شبرا الخيمه","ارض نوبار"),
            ("المساكن","الوحدة"),
            ("الملف شبرا الخيمه","الملف"),
            ("الوحدة عند المصانع","الوحدة"),
            ("خلف الكنيسة","شبرا البلد"),
            ("شارع زكريا","شبرا البلد"),
            ("منية السيرج","اخرى"),
        })aliases[Key(alias)]=canonical;
        return aliases;
    }
    private static string Key(string value)=>Regex.Replace(value,@"[\s\u200e\u200f\u061c\u200b\u064b-\u065f\u0670\u0640]", "")
        .Replace('أ','ا').Replace('إ','ا').Replace('آ','ا').Replace('ٱ','ا').Replace('ى','ي').Replace('ی','ي').Replace('ة','ه');
    public static string Canonical(string? value)=>string.IsNullOrWhiteSpace(value) ? "" : Aliases.TryGetValue(Key(value),out var canonical) ? canonical : "اخرى";
}
