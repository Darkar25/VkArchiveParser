using AngleSharp.Html.Parser;
using System.ComponentModel.Composition;
using VkArchiveParser.Categories;
using VkArchiveParser.Providers;
using VkArchiveParser.Utils;

namespace VkArchiveParser.Messages
{
    public class MessagesCategoryProvider : ICategoryProvider
    {
        public ICategory LoadCategory(string path, VkArchive archive) => new MessagesCategory(path, archive);

        public bool ProbeCategory(string path)
        {
            try
            {
                var a = Path.GetFileName(path);
                var b = Directory.EnumerateDirectories(path).Select(x => Path.GetFileName(x));
                if (Directory.GetFiles(path, "index-*.html").First().PathHtml().GetElementsByClassName("ui_crumb").Last().TextContent.Contains("Сообщения") &&
                    b.All(x => int.TryParse(x, out _))) {
                    return true;
                }
            } catch (Exception e) {
                return false;
            }
            return false;
        }
    }
}