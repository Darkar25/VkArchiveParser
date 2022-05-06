using AngleSharp;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

namespace VkArchiveParser.Utils
{
    public static class DirectoryUtils
    {
        public static IEnumerable<string> GetDirectories(string path)
        {
            foreach (var p in Directory.GetDirectories(path))
                yield return p.Remove(0, path.Length).Trim(Path.DirectorySeparatorChar);
        }

        public static IEnumerable<string> GetFiles(string path)
        {
            foreach (var p in Directory.GetFiles(path))
                yield return p.Remove(0, path.Length).Trim(Path.DirectorySeparatorChar);
        }

        public static IHtmlDocument PathHtml(this string file)
        {
            var s = new FileStream(file, FileMode.Open);
            var doc = new HtmlParser(new()
            {
                IsScripting = false,
                IsNotConsumingCharacterReferences = true,
                IsNotSupportingFrames = true,
                IsSupportingProcessingInstructions = false,
                IsKeepingSourceReferences = false
            }, BrowsingContext.New(Configuration.Default)).ParseDocument(s);
            s.Close();
            return doc;
        }

        public static IHtmlDocument StringHtml(this string content)
        {
            return new HtmlParser(new()
            {
                IsScripting = false,
                IsNotConsumingCharacterReferences = true,
                IsNotSupportingFrames = true,
                IsSupportingProcessingInstructions = false,
                IsKeepingSourceReferences = false
            }, BrowsingContext.New(Configuration.Default)).ParseDocument(content);
        }
    }
}