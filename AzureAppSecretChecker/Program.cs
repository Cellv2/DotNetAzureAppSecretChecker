using Azure.Identity;
using Microsoft.Graph;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AzureAppSecretChecker
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                const string tenantId = "";
                const string clientId = "";
                const string clientSecret = "";

                GetSecretExpiriesAndProcessResultsAsync(tenantId, clientId, clientSecret, Display).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
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
                var allApplications = await graphClient.Applications.Request().GetAsync();

                foreach (var app in allApplications)
                {
                    var passwordCredentials = app.PasswordCredentials;
                    if (passwordCredentials.Any())
                    {
                        // there can be more than one credential in each app
                        foreach (PasswordCredential passwordCredential in passwordCredentials)
                        {
                            JObject obj = new JObject();
                            obj.Add("TenantId", tenantId);
                            obj.Add("ApplicationId", app.AppId);
                            obj.Add("ObjectId", app.Id);
                            obj.Add("Domain", app.PublisherDomain);
                            //descirption
                            obj.Add("DisplayName", passwordCredential.DisplayName);
                            obj.Add("EndDateTime", passwordCredential.EndDateTime);
                            // first 3 characters of the secret
                            obj.Add("SecretHint", passwordCredential.Hint);
                            processResult(obj);
                        }
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
