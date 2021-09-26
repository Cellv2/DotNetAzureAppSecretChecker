using AzureAppSecretChecker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureAppSecretChecker.Cli
{
    public interface IDisplay
    {
        void ProcessAndDisplayAppSecrets(List<AzureAppSecretInfo> appSecretInfos);
    }
}
