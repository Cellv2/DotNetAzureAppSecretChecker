using Azure.Identity;
using AzureAppSecretChecker.Models;
using Microsoft.Graph;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using AzureAppSecretChecker.Services;
using AzureAppSecretChecker.Cli;

namespace AzureAppSecretChecker
{
    class Program
    {
        static void Main(string[] args)
        {
            IMicrosoftGraphService microsoftGraphService = new MicrosoftGraphService();
            IDisplay display = new Display();

            List<AzureAppCredential> azureAppCredentials = new List<AzureAppCredential>();
            azureAppCredentials.Add(new AzureAppCredential { ClientId = "", ClientSecret = "", TenantId = "" });

            foreach (AzureAppCredential azureAppCredential in azureAppCredentials)
            {
                try
                {
                    // TODO: think about trying to avoid multiple calls to a single tenant
                    List<AzureAppSecretInfo> passwordCredentials = microsoftGraphService.GetAppSecretInfoAsync(azureAppCredential).GetAwaiter().GetResult();
                    display.ProcessAndDisplayAppSecrets(passwordCredentials);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.Message);
                    Console.ResetColor();
                }
            }


            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}
