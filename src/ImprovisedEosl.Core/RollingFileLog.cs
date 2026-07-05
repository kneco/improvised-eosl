using System.IO;
using System.Text;

namespace ImprovisedEosl.Core;

public sealed class RollingFileLog
{
    public const long DefaultMaxBytes = 5 * 1024 * 1024;

    private readonly object _lock = new();
    private readonly long _maxBytes;
    private readonly string _path;

    public RollingFileLog(string path, long maxBytes = DefaultMaxBytes)
    {
        if (maxBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxBytes));
        }

        _path = path;
        _maxBytes = maxBytes;
    }

    public string Path => _path;

    public string BackupPath => _path + ".1";

    public void AppendLine(string line)
    {
        var text = line + Environment.NewLine;
        var incomingBytes = Encoding.UTF8.GetByteCount(text);

        lock (_lock)
        {
            var directory = System.IO.Path.GetDirectoryName(_path)
                ?? throw new InvalidOperationException("Log path must have a directory.");
            Directory.CreateDirectory(directory);

            if (File.Exists(_path) && new FileInfo(_path).Length + incomingBytes > _maxBytes)
            {
                File.Move(_path, BackupPath, overwrite: true);
            }

            File.AppendAllText(_path, text, Encoding.UTF8);
        }
    }
}
