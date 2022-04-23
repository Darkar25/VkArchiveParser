using CommandLine;
using CommandLine.Text;
using VkArchiveParser;
using VkArchiveParser_CLI;

#if DEBUG
var opts = new Options();
opts.Format = Options.NewFormat.JSON;
opts.UnzipPath = "./unzip_archive/";
opts.OriginalArchivePath = "./archive.zip";
opts.ParsedArchivePath = "./parsed_archive/";
opts.Merged = false;
Run(opts);
#else
var res = new Parser(with =>
{
    with.CaseInsensitiveEnumValues = true;
}).ParseArguments<Options>(args);
res.WithParsed(Run)
  .WithNotParsed((e) => Console.WriteLine(HelpText.AutoBuild(res)));
#endif

void Run(Options opts)
{
    VkArchive.ResolveOriginalPath = () =>
    {
        if(opts.UnzipPath is not null) return opts.UnzipPath;
        else
        {
            Console.WriteLine("Перезапустите программу, добавив флаг -u / --unzip с путём к папке для распаковки zip архива.");
            Environment.Exit(1);
        }
        return null;
    };
    DateTime startTime = DateTime.Now;
    VkArchive.UnzipProgress = new Progress<(int total, int current, string item)>((a) =>
    {
        var elapsedTime = DateTime.Now - startTime;
        var current = a.current == 0 ? 1 : a.current;
        var ETA = (elapsedTime * ((double)a.total / (double)current)) - elapsedTime;
        Console.WriteLine($"Распаковка {a.current}/{a.total} ({Math.Round((double)a.current / (double)a.total * 100d, 2)}%): {a.item}; ETA: {ETA.Hours}h {ETA.Minutes}m {ETA.Seconds}s");
    });
    VkArchive archive = new(opts.OriginalArchivePath);
    archive.ParsedPath = opts.ParsedArchivePath;
    foreach (var c in archive.Categories)
    {
        if (opts.Exclude is not null && opts.Exclude.Contains(c.CodeName))
        {
            Console.WriteLine("Категория " + c.DisplayName + " пропускается, так как она помечена как исключённая...");
            continue;
        }
        startTime = DateTime.Now;
        c.ConvertProgress = new Progress<(int total, int current, string item)>((a) =>
        {
            var elapsedTime = DateTime.Now - startTime;
            var current = a.current == 0 ? 1 : a.current;
            var ETA = (elapsedTime * ((double)a.total / (double)current)) - elapsedTime;
            Console.WriteLine($"{c.CodeName} {a.current}/{a.total} ({Math.Round((double)a.current / (double)a.total * 100d, 2)}%): {a.item}; ETA: {ETA.Hours}h {ETA.Minutes}m {ETA.Seconds}s");
        });
        Console.WriteLine("Подготовка категории " + c.DisplayName + "...");
        switch(opts.Format)
        {
            case Options.NewFormat.JSON:
                c.ConvertToJSON(opts.Merged);
                break;
            case Options.NewFormat.CSV:
                c.ConvertToCSV(opts.Merged);
                break;
            case Options.NewFormat.HTML:
                c.ConvertToHTML(opts.Merged);
                break;
            default:
                Console.WriteLine("Не знаю как ты это сделал, но больше так не делай >.<");
                Console.WriteLine("Поддерживаемые форматы для конвертирования: JSON, CSV, HTML");
                Environment.Exit(1);
                break;
        }
        
    }
}