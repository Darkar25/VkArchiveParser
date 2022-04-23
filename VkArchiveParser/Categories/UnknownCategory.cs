namespace VkArchiveParser.Categories
{
    public class UnknownCategory : ICategory
    {
        public UnknownCategory(string path, VkArchive archive) : base(path, archive) { }
        public override string CodeName => "unk";
        public override string DisplayName => "Неизвестно";
    }
}