namespace VkArchiveParser.Categories
{
    public abstract class ICategory
    {
        public ICategory(string path, VkArchive archive) {
            InputPath = path;
            Parent = archive;
        }
        public abstract string CodeName { get; }
        public abstract string DisplayName { get; }
        public IProgress<(int total, int processed, string currentItem)>? ConvertProgress;
        public VkArchive Parent { get; }
        public string InputPath { get; protected set; }
        protected int? _count;
        public virtual int Count => _count ??= Directory.GetFiles(InputPath).Length;
        public virtual void ConvertToJSON(bool merged = false) {
            var folder = Path.Combine(Parent.ParsedPath, Path.GetFileName(InputPath));
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder,"NotImplemented.json"),@"{""error"":""This category does not support JSON converting""}");
        }
        public virtual void ConvertToCSV(bool merged = false) {
            var folder = Path.Combine(Parent.ParsedPath, Path.GetFileName(InputPath));
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "NotImplemented.csv"), @"This category does not support CSV converting");
        }
        public virtual void ConvertToHTML(bool merged = false) {
            var folder = Path.Combine(Parent.ParsedPath, Path.GetFileName(InputPath));
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "NotImplemented.html"), "<!DOCTYPE html>\n<html>\n<body>\nThis category does not support HTML converting\n</body>\n</html>");
        }
    }
}