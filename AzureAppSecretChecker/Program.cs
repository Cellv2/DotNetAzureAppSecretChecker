﻿using Azure.Identity;
using Microsoft.Graph;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureAppSecretChecker
{
    class Program
    {
        static void Main(string[] args)
        {

            List<AzureAppCredential> azureAppCredentials = new List<AzureAppCredential>();
            azureAppCredentials.Add(new AzureAppCredential { ClientId = "", ClientSecret = "", TenantId = "" });

            foreach (AzureAppCredential azureAppCredential in azureAppCredentials)
            {
                try
                {
                    // TODO: think about trying to avoid multiple calls to a single tenant
                    GetSecretExpiriesAndProcessResultsAsync(azureAppCredential.TenantId, azureAppCredential.ClientId, azureAppCredential.ClientSecret, Display).GetAwaiter().GetResult();
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

        private static async Task GetSecretExpiriesAndProcessResultsAsync(string tenantId, string clientId, string clientSecret, Action<JObject> processResult)
        {
            string[] scopes = new string[] { "https://graph.microsoft.com/.default" };

            try
            {
                var options = new TokenCredentialOptions
                {
                    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
                };

                // https://docs.microsoft.com/dotnet/api/azure.identity.clientsecretcredential
                var clientSecretCredential = new ClientSecretCredential(
                    tenantId, clientId, clientSecret, options);

                var graphClient = new GraphServiceClient(clientSecretCredential, scopes);

                // there should only ever be one match for a specific appId, so top 1 is taken
                // if the appId does not exist, then we should have an error by this point (in the clientSecretCredential creation)
                // required permissions: https://docs.microsoft.com/en-us/graph/api/application-list?view=graph-rest-1.0#permissions
                IGraphServiceApplicationsCollectionPage targetAppCollection = await graphClient.Applications
                    .Request()
                    .Header("ConsistencyLevel", "eventual")
                    // filtering does not care for upper or lower case. Filter docs: https://docs.microsoft.com/en-us/graph/aad-advanced-queries#application-properties
                    .Filter($"appId eq '{clientId}'")
                    .Top(1)
                    .GetAsync();


                var targetApp = targetAppCollection.FirstOrDefault();
                var passwordCredentials = targetApp.PasswordCredentials;
                if (passwordCredentials.Any())
                {
                    // there can be more than one credential in each app
                    foreach (PasswordCredential passwordCredential in passwordCredentials)
                    {
                        JObject obj = new JObject();
                        obj.Add("TenantId", tenantId);
                        obj.Add("ApplicationId", targetApp.AppId);
                        obj.Add("ObjectId", targetApp.Id);
                        obj.Add("Domain", targetApp.PublisherDomain);
                        // descirption
                        obj.Add("DisplayName", passwordCredential.DisplayName);
                        obj.Add("EndDateTime", passwordCredential.EndDateTime);
                        // first 3 characters of the secret
                        obj.Add("SecretHint", passwordCredential.Hint);
                        processResult(obj);
                    }
                }
            }
            catch (Exception ex) when (ex.Message.Contains("AADSTS70011"))
            {
                // Invalid scope. The scope has to be in the form "https://resourceurl/.default"
                // Mitigation: Change the scope to be as expected.
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Scope provided is not supported");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to call the API");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Console.ResetColor();
            }
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
