using System.IO.Compression;
using VkArchiveParser.Categories;
using VkArchiveParser.Zip;

namespace VkArchiveParser
{
    public class VkArchive
    {
        public static Func<string> ResolveOriginalPath;
        public string OriginalPath { get; set; }
        public string ParsedPath { get; set; } = Path.Combine(Environment.ProcessPath, "parsed_archive");
        public static IProgress<(int total, int processed, string currentItem)>? UnzipProgress;
        public List<ICategory> Categories = new();
        public VkArchive(string path)
        {
            if (!File.Exists(path) && !Directory.Exists(path)) throw new FileNotFoundException($@"File/Folder ""{path}"" was not found!");
            var attr = File.GetAttributes(path);
            if(attr.HasFlag(FileAttributes.Directory))
                OriginalPath = path;
            else
            {
                ZipArchive archive = new(new FileStream(path, FileMode.Open), ZipArchiveMode.Read);
                OriginalPath = ResolveOriginalPath?.Invoke() ?? Path.Combine(Environment.ProcessPath, "original_archive");
                MyZipFileExtensions.ExtractToDirectory(archive, OriginalPath, UnzipProgress, true);
            }
            foreach (var f in Directory.EnumerateDirectories(OriginalPath))
                Categories.Add(CategoryProviderManager.Probe(f, this));
        }
    }
}