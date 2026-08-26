namespace Api.Configuration;

public static class DotEnvLoader
{
    public static void Load(string? path = null)
    {
        var loader = DotNetEnv.Env.NoClobber();

        if (path is null)
        {
            loader.TraversePath().Load();
            return;
        }

        loader.Load(path);
    }
}