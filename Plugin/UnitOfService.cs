using Microsoft.Xrm.Sdk;
using System;

namespace LogicApps.Portals.Plugin
{
    public class UnitOfService : IUnitOfService
    {
        private readonly IServiceProvider _serviceProvider;
        private IPluginExecutionContext _pluginExecutionContext;
        private IOrganizationServiceFactory _serviceFactory;
        private IOrganizationService _organizationService;
        private ITracingService _tracingService;
        public UnitOfService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new NullReferenceException(nameof(serviceProvider));
        }
        public IPluginExecutionContext PluginExecutionContext
        {
            get
            {
                if (_pluginExecutionContext == null)
                    _pluginExecutionContext = (IPluginExecutionContext)_serviceProvider.GetService(typeof(IPluginExecutionContext));

                return _pluginExecutionContext;
            }
        }

        public IOrganizationService OrganizationService
        {
            get
            {
                if (_organizationService == null)
                {
                    if (_serviceFactory == null)
                        _serviceFactory = (IOrganizationServiceFactory)_serviceProvider.GetService(typeof(IOrganizationServiceFactory));

                    _organizationService = _serviceFactory.CreateOrganizationService(PluginExecutionContext.UserId);
                }
                return _organizationService;
            }
        }

        public ITracingService TracingService
        {
            get
            {
                if (_tracingService == null)
                    _tracingService = (ITracingService)_serviceProvider.GetService(typeof(ITracingService));

                return _tracingService;
            }
        }
    }
}
