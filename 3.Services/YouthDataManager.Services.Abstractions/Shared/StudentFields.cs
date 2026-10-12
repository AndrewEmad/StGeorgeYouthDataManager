using YouthDataManager.Domain.Enums;

namespace YouthDataManager.Shared.Service.Abstractions;

public static class StudentFields
{
    public static string? Validate(string? name, string? phone, string? secondary, string? address, string? area,
        string? college, string? year, string? father, string? notes, Gender gender, DateTime? birthday, int? month = null)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(phone)) return "Full name and phone are required.";
        var fields = new (string Name,string? Value,int Limit)[] {
            ("FullName",name,200),("Phone",phone,20),("SecondaryPhone",secondary,20),("Address",address,500),
            ("Area",area,100),("College",college,100),("AcademicYear",year,100),("ConfessionFather",father,200),("Notes",notes,2000) };
        foreach (var f in fields) if ((f.Value?.Trim().Length ?? 0) > f.Limit) return $"{f.Name} exceeds {f.Limit} characters.";
        if(PhoneFields.Canonical(phone)=="" || (!string.IsNullOrWhiteSpace(secondary) && PhoneFields.Canonical(secondary)==""))return "Enter valid primary and secondary phone numbers.";
        if (!Enum.IsDefined(gender)) return "Invalid gender.";
        if (birthday.HasValue && (birthday.Value.Date > BusinessTime.LocalNow.Date || birthday.Value.Year < 1900)) return "Invalid birth date.";
        if (!birthday.HasValue && month.HasValue && month is < 1 or > 12) return "Invalid birthday month.";
        return null;
    }
}
