namespace YouthDataManager.WebApi.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AllowRestrictedAccountAttribute : Attribute;
