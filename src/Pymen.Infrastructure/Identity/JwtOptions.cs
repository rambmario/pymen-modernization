using System.ComponentModel.DataAnnotations;

namespace Pymen.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "pymen";
    public string Audience { get; set; } = "pymen-api";

    [Required]
    [MinLength(32)]
    public string SigningKey { get; set; } = string.Empty;

    public int ExpirationMinutes { get; set; } = 60;
}
