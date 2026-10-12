using System.Text.RegularExpressions;
namespace YouthDataManager.Shared.Service.Abstractions;
public static class PhoneFields
{
    public static string Canonical(string? value)
    {
        if(string.IsNullOrWhiteSpace(value))return "";
        var text=string.Concat(value.Trim().Select(c=>c is >= '٠' and <= '٩' ? (char)('0'+c-'٠') : c is >= '۰' and <= '۹' ? (char)('0'+c-'۰') : c));
        if(!Regex.IsMatch(text,@"^\+?[0-9\s\uFEFF().-]+$"))return "";
        var digits=new string(text.Where(char.IsDigit).ToArray());
        if(text.StartsWith('+')) { }
        else if(digits.StartsWith("00"))digits=digits[2..];
        else if(digits.StartsWith('0') && digits.Length==11)digits="2"+digits;
        else if(digits.StartsWith('1') && digits.Length==10)digits="20"+digits;
        return digits.Length is >=7 and <=15 ? digits : "";
    }
}
