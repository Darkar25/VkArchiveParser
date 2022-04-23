using VkArchiveParser.Categories;

namespace VkArchiveParser.Providers
{
    public interface ICategoryProvider
    {
        public ICategory LoadCategory(string path, VkArchive archive);
        public bool ProbeCategory(string path);
    }
}