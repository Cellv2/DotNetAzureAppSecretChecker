using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureAppSecretChecker.Models
{
    public class AzureAppSecretInfo
    {
        public string TenantId { get; set; }
        public string ApplicationId { get; set; }
        public string ObjectId { get; set; }
        public string Domain { get; set; }
        public string DisplayName { get; set; } // descirption
        public string EndDateTime { get; set; }
        public string SecretHint { get; set; } // first 3 characters of the secret
    }
}
