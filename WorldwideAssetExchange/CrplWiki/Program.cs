using AngleSharp;
using AngleSharp.Dom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace CrplWiki
{
    class Program
    {
         static async Task Main(string[] args)
        {
            await FetchLuck("1099512958806");
        }

        static async Task FetchLuck(string landId)
        {
            List<string[]> data;
            if (LandIsCached(landId))
            {
                var cache = GetCache(landId);
                data = ReadCache(cache);
            }
            else
            {
                data = await DownloadNewData(landId);
                var serialized = JsonSerializer.Serialize(data);
                File.WriteAllText(Environment.CurrentDirectory + landId, serialized);
            }
            var minerTest = new MinerData(data[1]);
            await DownloadTools(minerTest.GetTools());
            foreach (var tool in minerTest.GetTools())
            {
                var json = JsonSerializer.Serialize(tool.Asset);
                File.WriteAllText(Environment.CurrentDirectory + "\\tool-{x}.json", json);
            }
        }

        static async Task<List<string[]>> DownloadNewData(string landId)
        {
            var url = @"http://awstats.io/mining/land/24/" + landId;
            var config = Configuration.Default.WithDefaultLoader();
            var address = url;
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(address);
            var cellSelector = "table";
            IElement cells = document.QuerySelector(cellSelector);
            IHtmlCollection<IElement> tableRows = cells.QuerySelectorAll(">tr");
            var minerData = tableRows.ToList();
            var parsedData = SplitLinks(minerData);
            return parsedData;
        }

        static List<string[]> ReadCache(string cache)
        {
            return JsonSerializer.Deserialize<List<string[]>>(cache);
        }

        static string GetCache(string landId)
        {
            return File.ReadAllText(Environment.CurrentDirectory + landId);
        }

        static bool LandIsCached(string landId)
        {
            if (File.Exists(Environment.CurrentDirectory + landId)) 
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        static async Task DownloadTools(AwTool[] tools)
        {
            foreach(var tool in tools)
            {
                await tool.CreateFromBoksViewer();
            }
        }

        static List<string[]> SplitLinks(IEnumerable<IElement> rows)
        {
            var parsed = new List<string[]>();
            foreach(var row in rows)
            {
                parsed.Add(row.TextContent.Split('\n', StringSplitOptions.None));
            }
            return parsed;
        }


        static async Task FetchWiki()
        {
            var url = @"https://knucklecracker.com/wiki/doku.php?id=4rpl:start";
            var config = Configuration.Default.WithDefaultLoader();
            var address = url;
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(address);
            var cellSelector = "a";
            var cells = document.QuerySelectorAll(cellSelector);
            var links = cells.Where(m => m.ClassName == "wikilink1");
            var commands = links.Where(m => m.GetAttribute("Href").Contains("commands:"));
        }

        static async Task FetchCommand(IElement element)
        {
            var link = element.GetAttribute("Href");

        }


    }
}
