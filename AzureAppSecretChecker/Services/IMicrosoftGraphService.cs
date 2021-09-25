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
    public interface IMicrosoftGraphService
    {
        Task<List<PasswordCredential>> GetSecretExpiriesAsync(AzureAppCredential azureAppCredential, Action<JObject> processResult);
        Task GetSecretExpiriesAsync(List<AzureAppCredential> azureAppCredentials);
    }
}
