using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace LogicApps.Portals.Plugin.PortalQuery
{
    public class PortalQuery : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
                throw new NullReferenceException(nameof(serviceProvider));

            IUnitOfService unitOfService = new UnitOfService(serviceProvider);
            CheckPlugin(unitOfService, GlobalConfiguration.EntityName, GlobalConfiguration.Message, GlobalConfiguration.PluginStage);

            if (unitOfService.PluginExecutionContext.InputParameters["Query"] is FetchExpression query)
            {
                QueryService queryService = new QueryService(unitOfService, query);
                ;

                EntityCollection result = queryService.GetData();

                if (result != null)
                    unitOfService.PluginExecutionContext.OutputParameters["BusinessEntityCollection"] = result;
            }
        }

        private void CheckPlugin(IUnitOfService unitOfService, string entityName = null, string message = null, PluginStage? stage = null)
        {
            if (!string.IsNullOrEmpty(entityName) && unitOfService.PluginExecutionContext.PrimaryEntityName != entityName)
                throw new ArgumentException($"Required entity name is {entityName}");


            if (!string.IsNullOrEmpty(message) && unitOfService.PluginExecutionContext.MessageName != message)
            {
                throw new ArgumentException($"Required message name is {message}");
            }

            if (stage != null && unitOfService.PluginExecutionContext.Stage != (int)stage)
            {
                throw new ArgumentException($"Required stage is {nameof(stage)}");
            }
        }
    }
}
