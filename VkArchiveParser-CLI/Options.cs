using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VkArchiveParser_CLI
{
    internal class Options
    {
        [Value(0, MetaName = "input", Required = true, HelpText = "Путь к архиву ВКонтакте: папка или zip файл")]
        public string OriginalArchivePath { get; set; }
        [Option('o', "output", Required = true, HelpText = "Путь для сохранения архива в новом формате")]
        public string ParsedArchivePath { get; set; }
        [Option('u', "unzip", Required = false, HelpText = "Путь для распаковки zip файла")]
        public string UnzipPath { get; set; }
        [Option('f', "format", Required = true, HelpText = "Формат в который необходимо конвертировать архив")]
        public NewFormat Format { get; set; }
        [Option('e', "exclude", Required = false, HelpText = "Категории, которые следует исключить из нового архива")]
        public IEnumerable<string> Exclude { get; set; }
        [Option('m', "merged", Required = false, Default = false, HelpText = "При возможности слить файлы категории воедино")]
        public bool Merged { get; set; }
        public enum NewFormat
        {
            JSON, CSV, HTML
        }
    }
}
