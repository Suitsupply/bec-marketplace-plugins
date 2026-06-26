using System.Text;

namespace Template.App.Extensions;

public static class StreamExtensions
{
    public static async Task<string> ReadStreamAsStringAsync(this Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }
}