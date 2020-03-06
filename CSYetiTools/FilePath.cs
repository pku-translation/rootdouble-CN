using System;
using System.IO;

namespace CsYetiTools
{
    public class FilePath
    {
        private string _path;

        public FilePath(string path)
        {
            _path = path;
        }

        public static implicit operator FilePath(string path)
            => new FilePath(path);

        public static implicit operator string(FilePath path)
            => path._path;

        public static FilePath operator / (FilePath path1, FilePath path2)
            => Path.Combine(path1, path2);

        public static FilePath operator + (FilePath path1, FilePath path2)
            => path1._path + path2._path;

        public FilePath Parent
            => Path.GetDirectoryName(_path) ?? throw new InvalidOperationException($"Cannot get parent of \"{_path}\"");

        public FilePath FileName
            => Path.GetFileName(_path);
        
        public FilePath Extention
            => Path.GetExtension(_path);

        public FilePath ToRelative(string parent)
            => Path.GetRelativePath(parent, _path);

        public FilePath ToRelative()
            => ToRelative(Directory.GetCurrentDirectory());

        public FilePath ToAbsolute()
            => Path.GetFullPath(_path);

        public FilePath FromEnvironment(string key)
        {
            var value = Environment.GetEnvironmentVariable(key);
            if (value == null) throw new ArgumentException($"Cannot find env variable \"{key}\"");
            return value;
        }

        public override string ToString()
        {
            return _path.Replace("\\", "/");
        }
    }
}