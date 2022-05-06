using System.ComponentModel.Composition;
using VkArchiveParser.Categories;

namespace VkArchiveParser.Providers
{
    [InheritedExport]
    public interface ICategoryProvider
    {
        public ICategory LoadCategory(string path, VkArchive archive);
        public bool ProbeCategory(string path);
    }
}