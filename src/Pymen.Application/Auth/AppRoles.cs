namespace Pymen.Application.Auth;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Planner = "Planner";
    public const string Operator = "Operator";

    public static readonly string[] All = [Admin, Planner, Operator];
}
