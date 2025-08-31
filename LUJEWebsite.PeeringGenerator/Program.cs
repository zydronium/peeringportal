using LUJEWebsite.Library.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace LUJEWebsite.PeeringGenerator
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            string configFile = File.ReadAllText("peeringconfig.json");
            Config config = JsonConvert.DeserializeObject<Config>(configFile);

			Console.WriteLine("Running a filter batch");
            Filter.Run(config);
            Console.WriteLine("Running a ASN batch");
			await ASNObjectGenerator.RunAsync();
			Console.WriteLine("Running a AS-SET batch");
			await ASSETObjectGenerator.RunAsync();
			Console.WriteLine("Finished");
		}
    }
}