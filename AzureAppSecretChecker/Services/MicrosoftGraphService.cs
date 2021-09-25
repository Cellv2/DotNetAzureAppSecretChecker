using Azure.Identity;
using AzureAppSecretChecker.Models;
using Microsoft.Graph;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureAppSecretChecker.Services
{
    public class MicrosoftGraphService : IMicrosoftGraphService
    {
        // Please note that this may still return multiple secrets
        // TODO: pretty sure that MsGraph always returns the hint? Wasn't it only AadGraph which doesn't?
        public async Task<List<PasswordCredential>> GetSecretExpiriesAsync(AzureAppCredential azureAppCredential, Action<JObject> processResult)
        {
            string tenantId = azureAppCredential.TenantId;
            string clientId = azureAppCredential.ClientId;
            string clientSecret = azureAppCredential.ClientSecret;

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

                List<PasswordCredential> appSecrets = new List<PasswordCredential>();

                foreach (Application app in allApplications)
                {
                    var passwordCredentials = app.PasswordCredentials;
                    if (app.AppId == clientId && passwordCredentials.Any())
                    {
                        // there can be more than one credential in each app
                        foreach (PasswordCredential passwordCredential in passwordCredentials)
                        {
                            appSecrets.Add(passwordCredential);

                            // TODO: Remove the processResult and move it somewhere more sensible
                            JObject obj = new JObject();
                            obj.Add("TenantId", tenantId);
                            obj.Add("ApplicationId", app.AppId);
                            obj.Add("ObjectId", app.Id);
                            obj.Add("Domain", app.PublisherDomain);
                            // descirption
                            obj.Add("DisplayName", passwordCredential.DisplayName);
                            obj.Add("EndDateTime", passwordCredential.EndDateTime);
                            // first 3 characters of the secret
                            obj.Add("SecretHint", passwordCredential.Hint);
                            processResult(obj);
                        }
                    }
                }

                return appSecrets;
            }
            catch (Exception ex) when (ex.Message.Contains("AADSTS70011"))
            {
                // Invalid scope. The scope has to be in the form "https://resourceurl/.default"
                // Mitigation: Change the scope to be as expected.
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Scope provided is not supported");
                Console.ResetColor();

                return null;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to call the API");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Console.ResetColor();

                return null;
            }
        
    }

        public async Task GetSecretExpiriesAsync(List<AzureAppCredential> azureAppCredentials)
        {
            throw new NotImplementedException();
        }
    }
}
