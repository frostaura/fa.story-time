using System.Text.Json;

namespace StoryTime.Api.Services;

internal static class JsonFileStateStore
{
    public static string ResolvePath(string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }

        return Path.Combine(AppContext.BaseDirectory, configuredPath);
    }

    public static T Load<T>(string path, T fallback)
    {
        if (!File.Exists(path))
        {
            return fallback;
        }

        var raw = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return fallback;
        }

        return JsonSerializer.Deserialize<T>(raw) ?? fallback;
    }

    public static void Save<T>(string path, T value)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempFilePath = $"{path}.{Guid.NewGuid():N}.tmp";
        File.WriteAllText(tempFilePath, JsonSerializer.Serialize(value));
        File.Move(tempFilePath, path, overwrite: true);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }
}
