using Microsoft.Xrm.Sdk;

namespace LogicApps.Portals.Plugin.Contact
{
    public class SetCustomId : PluginBase
    {
        public SetCustomId() : base(typeof(SetCustomId))
        {
            RegisterEvent(PipelineStages.PostOperation, MessageNames.Create,"contact", PostOperationCreateHandler);
 
        }
        private void PostOperationCreateHandler(LocalPluginContext localPluginContext)
        {
            IPluginExecutionContext context = localPluginContext.PluginExecutionContext;
            IOrganizationService organizationService = localPluginContext.OrganizationService;

            var target = localPluginContext.Target<Entity>();
            var contact = new Entity("contact", target.Id);
            contact["new_recordid"] = target.Id.ToString();

            organizationService.Update(contact);
        }
    }
}
