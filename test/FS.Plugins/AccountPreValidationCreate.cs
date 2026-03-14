using Microsoft.Xrm.Sdk;
using SparkleXrm.Tasks;

namespace FS.Plugins
{
    [CrmPluginRegistration(
        "Create",
        "account",
        StageEnum.PreValidation,
        ExecutionModeEnum.Synchronous,
        "",                                         // filtering attributes: empty = all (PreValidation Create has no target attributes to filter)
        "FS.Plugins: Pre-Validation Create Account",
        1,
        IsolationModeEnum.Sandbox,
        Description = "Pre-validation plugin for Account Create"
    )]
    public class AccountPreValidationCreate : PluginBase
    {
        public AccountPreValidationCreate(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(AccountPreValidationCreate)) { }

        protected override void ExecuteDataversePlugin(ILocalPluginContext localPluginContext)
        {
            var context = localPluginContext.PluginExecutionContext;
            var service = localPluginContext.OrganizationService;

            if (context.InputParameters.TryGetValue("Target", out var targetObj)
                && targetObj is Entity target)
            {
                localPluginContext.Trace($"Processing account Create. Target Id={target.Id}");

                // TODO: implement plugin logic here
                // Example: validate that the account name is not empty
                // var name = target.GetAttributeValue<string>("name");
                // if (string.IsNullOrWhiteSpace(name))
                //     throw new InvalidPluginExecutionException("Account name is required.");
            }
        }
    }
}
