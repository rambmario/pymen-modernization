namespace Pymen.Infrastructure.Persistence;

/// <summary>
/// Minimal .env loader for local development (docker-compose and API/tests).
/// </summary>
public static class DotEnv
{
    public static void LoadFromAncestorDirectory(string startDirectory, string fileName = ".env")
    {
        var dir = new DirectoryInfo(startDirectory);
        while (dir is not null)
        {
            var path = Path.Combine(dir.FullName, fileName);
            if (File.Exists(path))
            {
                Load(path);
                return;
            }

            dir = dir.Parent;
        }
    }

    public static void Load(string path)
    {
        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#') || !line.Contains('='))
            {
                continue;
            }

            var separator = line.IndexOf('=');
            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim().Trim('"').Trim('\'');
            if (key.Length == 0)
            {
                continue;
            }

            // Do not override variables already set by the host/CI.
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    public static string RequireSqlPassword()
    {
        var password = Environment.GetEnvironmentVariable("MSSQL_SA_PASSWORD");
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "MSSQL_SA_PASSWORD is not set. Copy .env.example to .env and set the password.");
        }

        return password;
    }

    public static string BuildLocalSqlConnectionString(string database, int connectTimeoutSeconds = 30)
    {
        var password = RequireSqlPassword();
        return
            $"Server=localhost,14333;Database={database};User Id=sa;Password={password};TrustServerCertificate=True;Encrypt=False;Connect Timeout={connectTimeoutSeconds}";
    }
}
