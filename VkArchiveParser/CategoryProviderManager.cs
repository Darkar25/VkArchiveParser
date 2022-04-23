using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using VkArchiveParser.Categories;
using VkArchiveParser.Providers;

namespace VkArchiveParser
{
    public static class CategoryProviderManager
    {
        public static CompositionContainer _container;
        static CategoryProviderManager()
        {
            try
            {
                // Basic MEF setup
                var catalog = new AggregateCatalog();
                catalog.Catalogs.Add(new AssemblyCatalog(typeof(CategoryProviderManager).Assembly));
                catalog.Catalogs.Add(new DirectoryCatalog(Environment.CurrentDirectory));
                _container = new CompositionContainer(catalog);
                Providers = _container.GetExportedValues<ICategoryProvider>();
            }
            catch (CompositionException compositionException)
            {
                Console.WriteLine(compositionException.ToString());
            }
        }
        public static IEnumerable<ICategoryProvider> Providers { get; set; }
        public static ICategory Probe(string path, VkArchive archive)
        {
            foreach (var r in Providers)
                if (r.ProbeCategory(path))
                    return r.LoadCategory(path, archive);
            return new UnknownCategory(path, archive);
        }
    }
}