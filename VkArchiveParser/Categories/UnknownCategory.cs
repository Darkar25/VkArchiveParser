namespace VkArchiveParser.Categories
{
    public class UnknownCategory : ICategory
    {
        public UnknownCategory(string path, VkArchive archive) : base(path, archive) { }
        public override string CodeName => "unknown";
        public override string DisplayName => $"{Folder} (Неизвестно)";
    }
}