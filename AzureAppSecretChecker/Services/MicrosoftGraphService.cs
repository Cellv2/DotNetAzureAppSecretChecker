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
        public async Task<List<AzureAppSecretInfo>> GetAppSecretInfoAsync(AzureAppCredential azureAppCredential)
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

                List<AzureAppSecretInfo> appSecrets = new List<AzureAppSecretInfo>();

                foreach (Application app in allApplications)
                {
                    var passwordCredentials = app.PasswordCredentials;
                    if (app.AppId == clientId && passwordCredentials.Any())
                    {
                        // there can be more than one credential in each app
                        foreach (PasswordCredential passwordCredential in passwordCredentials)
                        {
                            string stringEndDateTime = passwordCredential.EndDateTime.HasValue ? passwordCredential.EndDateTime.Value.ToString() : "";

                            appSecrets.Add(new AzureAppSecretInfo
                            {
                                ApplicationId = app.AppId,
                                DisplayName = passwordCredential.DisplayName,
                                Domain = app.PublisherDomain,
                                EndDateTime = stringEndDateTime,
                                ObjectId = app.Id,
                                SecretHint = passwordCredential.Hint,
                                TenantId = tenantId
                            });
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
    }
}
