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

            if (tenantId.Length == 0 || tenantId == null) { throw new Exception("TenantId was empty or null. Please provide a valid TenantId"); }
            if (clientId.Length == 0 || clientId == null) { throw new Exception("ClientId was empty or null. Please provide a valid ClientId"); }
            if (clientSecret.Length == 0 || clientSecret == null) { throw new Exception("ClientSecret was empty or null. Please provide a valid ClientSecret"); }

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
                if (targetApp == null) { throw new Exception($"The connection to MS Graph was successful, but the requested clientId ({clientId}) was not found"); }


                var passwordCredentials = targetApp.PasswordCredentials;
                List<AzureAppSecretInfo> appSecrets = new List<AzureAppSecretInfo>();
                if (passwordCredentials.Any())
                {
                    // there can be more than one credential in each app
                    foreach (PasswordCredential passwordCredential in passwordCredentials)
                    {
                        AzureAppSecretInfo azureAppSecretInfo = new AzureAppSecretInfo
                        {
                            ApplicationId = targetApp.AppId,
                            DisplayName = passwordCredential.DisplayName, // descirption
                            Domain = targetApp.DisplayName,
                            EndDateTime = passwordCredential.EndDateTime.ToString(),
                            ObjectId = targetApp.Id,
                            SecretHint = passwordCredential.Hint, // first 3 characters of the secret
                            TenantId = tenantId
                        };

                        appSecrets.Add(azureAppSecretInfo);
                    }
                }
                else
                {
                    AzureAppSecretInfo azureAppWithoutSecrets = new AzureAppSecretInfo
                    {
                        ApplicationId = targetApp.AppId,
                        DisplayName = "No app secrets found", // descirption
                        Domain = targetApp.DisplayName,
                        EndDateTime = "No app secrets found",
                        ObjectId = targetApp.Id,
                        SecretHint = "No app secrets found", // first 3 characters of the secret
                        TenantId = tenantId
                    };

                    appSecrets.Add(azureAppWithoutSecrets);
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
