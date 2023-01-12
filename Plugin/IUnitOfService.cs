using Microsoft.Xrm.Sdk;

namespace LogicApps.Portals.Plugin
{
    public interface IUnitOfService
    {
        IPluginExecutionContext PluginExecutionContext { get; }
        IOrganizationService OrganizationService { get; }
        ITracingService TracingService { get; }
    }
}
