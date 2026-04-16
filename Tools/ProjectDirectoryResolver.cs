namespace NetForge;

public partial class Program
{
    public static string ResolveProjectRoot()
    {
        DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir != null)
        {
            bool looksLikeRoot =
                File.Exists(Path.Combine(dir.FullName, "NetForge.sln")) ||
                (Directory.Exists(Path.Combine(dir.FullName, "Networking")) &&
                 Directory.Exists(Path.Combine(dir.FullName, "Tools")));

            if (looksLikeRoot)
                return dir.FullName;

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not resolve NetForge project root from: {AppContext.BaseDirectory}");//Commit trigger
    }
}