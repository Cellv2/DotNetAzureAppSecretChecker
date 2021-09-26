using AzureAppSecretChecker.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureAppSecretChecker.Cli
{
    public class Display : IDisplay
    {
        public void ProcessAndDisplayAppSecrets(List<AzureAppSecretInfo> appSecretInfos)
        {
            if (!appSecretInfos.Any())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Result did not contain any information. This probably shouldn't happen");
                Console.ResetColor();

                return;
            }

            foreach (AzureAppSecretInfo appSecretInfo in appSecretInfos)
            {
                OutputSecretInfoToConsole(JObject.FromObject(appSecretInfo));
            }

        }

        private void OutputSecretInfoToConsole(JObject result)
        {
            foreach (JProperty child in result.Properties().Where(p => !p.Name.StartsWith("@")))
            {
                Console.WriteLine($"{child.Name} = {child.Value}");
            }

            Console.WriteLine("-----");
        }
    }
}
