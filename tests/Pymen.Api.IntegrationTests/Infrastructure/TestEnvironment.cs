using Pymen.Infrastructure.Persistence;

namespace Pymen.Api.IntegrationTests.Infrastructure;

internal static class TestEnvironment
{
    private static readonly object Gate = new();
    private static bool _loaded;

    public static void EnsureLoaded()
    {
        lock (Gate)
        {
            if (_loaded)
            {
                return;
            }

            DotEnv.LoadFromAncestorDirectory(AppContext.BaseDirectory);
            DotEnv.LoadFromAncestorDirectory(Directory.GetCurrentDirectory());
            _loaded = true;
        }
    }

    public static string SqlPassword
    {
        get
        {
            EnsureLoaded();
            return DotEnv.RequireSqlPassword();
        }
    }
}
