using AngleSharp.Html.Dom;
using EasyJSON;
using System.Text;
using VkArchiveParser.Utils;

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
        protected string? _folder;
        public string Folder => _folder ??= Path.GetFileName(InputPath);
        protected int? _count;
        public virtual int Count => _count ??= Directory.GetFiles(InputPath).Length;
        public virtual void PopulateCurrentUserInfo() {
            if (Parent.CurrentUser["id"].Tag == JSONNodeType.None)
            {
                var doc = Directory.GetFiles(InputPath).First().PathHtml();
                var jd = doc.GetElementsByName("jd")[0] as IHtmlMetaElement;
                var b64 = Encoding.UTF8.GetString(Convert.FromBase64String(StringUtils.Base64FixPadding(jd.Content)));
                var b64j = JSON.Parse(b64);
                var currentUserId = b64j["user_id"].AsInt;
                Parent.CurrentUser["id"] = currentUserId;
            }
        }
        public virtual void ConvertToJSON(bool merged = false) {
            var folder = Path.Combine(Parent.ParsedPath, Folder);
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder,"NotImplemented.json"),@"{""error"":""This category does not support JSON converting""}");
        }
        public virtual void ConvertToCSV(bool merged = false) {
            var folder = Path.Combine(Parent.ParsedPath, Folder);
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "NotImplemented.csv"), @"This category does not support CSV converting");
        }
        // For module developers:
        // Please, include inhex.html or index-*.html file in root category folder so it is accessible from archive main page...
        public virtual void ConvertToHTML(bool merged = false) {
            var folder = Path.Combine(Parent.ParsedPath, Folder);
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "index-NotImplemented.html"), "<!DOCTYPE html>\n<html>\n<body>\nThis category does not support HTML converting\n</body>\n</html>");
        }
    }
}