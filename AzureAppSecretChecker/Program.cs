using Azure.Identity;
using AzureAppSecretChecker.Models;
using Microsoft.Graph;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using AzureAppSecretChecker.Services;

namespace AzureAppSecretChecker
{
    class Program
    {
        static void Main(string[] args)
        {

            IMicrosoftGraphService microsoftGraphService = new MicrosoftGraphService();

            List<AzureAppCredential> azureAppCredentials = new List<AzureAppCredential>();
            azureAppCredentials.Add(new AzureAppCredential { ClientId = "", ClientSecret = "", TenantId = "" });

            foreach (AzureAppCredential azureAppCredential in azureAppCredentials)
            {
                try
                {
                    // TODO: think about trying to avoid multiple calls to a single tenant
                    // TODO: Move display out of the msgraph service
                    List<PasswordCredential> passwordCredentials = microsoftGraphService.GetSecretExpiriesAsync(azureAppCredential, Display).GetAwaiter().GetResult();
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

        private static void Display(JObject result)
        {
            foreach (JProperty child in result.Properties().Where(p => !p.Name.StartsWith("@")))
            {
                Console.WriteLine($"{child.Name} = {child.Value}");
            }

            Console.WriteLine("-----");
        }

    }
}
