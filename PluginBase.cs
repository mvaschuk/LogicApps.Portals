using CrmEarlyBound;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.ServiceModel;

namespace LogicApps.Portals
{
    public class PluginBase : IPlugin
    {
        static PluginBase()
        {
            // Hook into ResolveAssembly event for external dependencies
            //AssemblyLoader.RegisterAssemblyLoader();
        }

        public class LocalPluginContext
        {
            private object _target;
            internal IServiceProvider ServiceProvider { get; private set; }

            public T GetInput<T>(string parameterName)
            {
                var context = PluginExecutionContext;
                if (!PluginExecutionContext.InputParameters.Contains(parameterName))
                {
                    throw new InvalidPluginExecutionException($"{parameterName} is empty");
                }

                var param = PluginExecutionContext.InputParameters[parameterName];
                if (param == null)
                {
                    throw new InvalidPluginExecutionException($"{parameterName} is null");

                }
                if (!(param is T))
                {
                    throw new InvalidPluginExecutionException($"{parameterName} is not of type {typeof(T).Name}. Real type is {param?.GetType().Name}");

                }
                return (T)param;
            }

            public T GetNullAbleInput<T>(string parameterName)
            {
                if (!(PluginExecutionContext.InputParameters?.Contains(parameterName) ?? false))
                {
                    return default;
                }

                var param = PluginExecutionContext.InputParameters[parameterName];

                if (param == null)
                {
                    return default;
                }
                if (!(param is T))
                {
                    throw new InvalidPluginExecutionException($"{parameterName} is not of type {typeof(T).Name}. Real type is {param?.GetType().Name}");

                }

                return (T)param;
            }

            public T GetOutput<T>(string parameterName)
            {

                if (!PluginExecutionContext.OutputParameters.Contains(parameterName))
                {
                    return default;
                }

                var param = PluginExecutionContext.OutputParameters[parameterName];

                if (!(param is T))
                {
                    throw new InvalidPluginExecutionException($"{parameterName} is not of type {typeof(T).Name}. Real type is {param?.GetType().Name}");

                }
                return (T)param;
            }

            public void SetOutput(string parameterName, object value)
            {
                if (PluginExecutionContext.OutputParameters.Contains(parameterName))
                {
                    PluginExecutionContext.OutputParameters[parameterName] = value;
                }
                else
                {
                    PluginExecutionContext.OutputParameters.Add(parameterName, value);
                }
            }

            public void SetOutput(OrganizationResponse response)
            {
                foreach (var item in response.Results)
                {
                    if (PluginExecutionContext.OutputParameters.Contains(item.Key))
                    {
                        PluginExecutionContext.OutputParameters[item.Key] = item.Value;
                    }
                    else
                    {
                        PluginExecutionContext.OutputParameters.Add(item.Key, item.Value);
                    }
                }
            }

            public IOrganizationService OrganizationService { get; private set; }

            public IOrganizationService SystemOrganizationService { get; private set; }
            public IPluginExecutionContext PluginExecutionContext { get; private set; }
            public ITracingService TracingService { get; private set; }

            public EntityReference TargetRef { get { return _target as EntityReference ?? (_target as Entity).ToEntityReference(); } }
            public T Target<T>() where T : Entity => (_target as Entity)?.ToEntity<T>();

            public T PostImage<T>(string imageName = "PostImage") where T : Entity => PluginExecutionContext.PostEntityImages[imageName].ToEntity<T>();
            public T PreImage<T>(string imageName = "PreImage") where T : Entity => PluginExecutionContext.PreEntityImages?.Contains(imageName) ?? false ? PluginExecutionContext.PreEntityImages[imageName].ToEntity<T>() : null;

            /// <summary>
            /// Indicates if action done/API called etc by User not System or App User(Azure Logic Apps, Function Apps)
            /// </summary>
            public bool IsInitiatingByUser
            {
                get => SystemOrganizationService
                    .Retrieve(SystemUser.EntityLogicalName, PluginExecutionContext.InitiatingUserId, new ColumnSet(SystemUser.Fields.IsLicensed))
                    .ToEntity<SystemUser>().IsLicensed ?? false;
            }
            public Guid InitiatingUserId { get => PluginExecutionContext.InitiatingUserId; }
            public Guid UserId { get => PluginExecutionContext.UserId; }
            private LocalPluginContext()
            {
            }

            public LocalPluginContext(IServiceProvider serviceProvider)
            {
                if (serviceProvider == null)
                {
                    throw new ArgumentNullException("serviceProvider");
                }

                // Obtain the execution context service from the service provider.
                PluginExecutionContext = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

                // Obtain the tracing service from the service provider.
                TracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

                // Obtain the Organization Service factory service from the service provider
                IOrganizationServiceFactory factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));

                // Use the factory to generate the Organization Service.
                OrganizationService = factory.CreateOrganizationService(PluginExecutionContext.UserId);
                SystemOrganizationService = factory.CreateOrganizationService(null);

                _target = GetNullAbleInput<object>("Target");
            }

            public void Trace(string message)
            {
                if (string.IsNullOrWhiteSpace(message) || TracingService == null)
                {
                    return;
                }

                if (PluginExecutionContext == null)
                {
                    TracingService.Trace(message);
                }
                else
                {
                    TracingService.Trace(
                        "{0}, Correlation Id: {1}, Initiating User: {2}",
                        message,
                        PluginExecutionContext.CorrelationId,
                        PluginExecutionContext.InitiatingUserId);
                }
            }
        }

        private Collection<Tuple<int, string, string, Action<LocalPluginContext>>> registeredEvents;

        /// <summary>
        /// Gets the List of events that the plug-in should fire for. Each List
        /// Item is a <see cref="Tuple"/> containing the Pipeline Stage, Message and (optionally) the Primary Entity. 
        /// In addition, the fourth parameter provide the delegate to invoke on a matching registration.
        /// </summary>
        protected Collection<Tuple<int, string, string, Action<LocalPluginContext>>> RegisteredEvents
        {
            get
            {
                if (registeredEvents == null)
                {
                    registeredEvents = new Collection<Tuple<int, string, string, Action<LocalPluginContext>>>();
                }

                return registeredEvents;
            }
        }

        protected void RegisterEvent(int pipelineStage, string messageName, string entityLogicalName, Action<LocalPluginContext> action)
        {
            RegisteredEvents.Add(new Tuple<int, string, string, Action<LocalPluginContext>>(pipelineStage, messageName, entityLogicalName, action));
        }

        /// <summary>
        /// Gets or sets the name of the child class.
        /// </summary>
        /// <value>The name of the child class.</value>
        protected string ChildClassName
        {
            get;

            private set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Plugin"/> class.
        /// </summary>
        /// <param name="childClassName">The <see cref=" cred="Type"/> of the derived class.</param>
        public PluginBase(Type childClassName)
        {
            ChildClassName = childClassName.ToString();
        }

        /// <summary>
        /// Executes the plug-in.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <remarks>
        /// For improved performance, Microsoft Dynamics CRM caches plug-in instances. 
        /// The plug-in's Execute method should be written to be stateless as the constructor 
        /// is not called for every invocation of the plug-in. Also, multiple system threads 
        /// could execute the plug-in at the same time. All per invocation state information 
        /// is stored in the context. This means that you should not use global variables in plug-ins.
        /// </remarks>
        public void Execute(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException("serviceProvider");
            }

            LocalPluginContext localcontext = new LocalPluginContext(serviceProvider);

            localcontext.Trace(string.Format(CultureInfo.InvariantCulture, "Entered {0}.Execute()", ChildClassName));
            localcontext.Trace("Depth = " + localcontext.PluginExecutionContext.Depth);
            localcontext.Trace("PrimaryEntityName = " + localcontext.PluginExecutionContext.PrimaryEntityName);
            localcontext.Trace("Target Id = " + localcontext.PluginExecutionContext.PrimaryEntityId);
            localcontext.Trace("MessageName = " + localcontext.PluginExecutionContext.MessageName);
            localcontext.Trace("Mode = " + localcontext.PluginExecutionContext.Mode);
            localcontext.Trace("Stage = " + localcontext.PluginExecutionContext.Stage);
            localcontext.Trace($"TimeZone = {TimeZone.CurrentTimeZone.StandardName} UTCOffSet = {TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now)}");


            try
            {
                // Iterate over all of the expected registered events to ensure that the plugin
                // has been invoked by an expected event
                // For any given plug-in event at an instance in time, we would expect at most 1 result to match.
                Action<LocalPluginContext> entityAction =
                    (from a in RegisteredEvents
                     where
                         a.Item1 == localcontext.PluginExecutionContext.Stage &&
                         a.Item2 == localcontext.PluginExecutionContext.MessageName &&
                         (string.IsNullOrWhiteSpace(a.Item3)
                             ? true
                             : MessageNames.Associate == localcontext.PluginExecutionContext.MessageName ?
                                a.Item3 == localcontext.GetNullAbleInput<Relationship>("Relationship")?.SchemaName
                                : a.Item3 == localcontext.PluginExecutionContext.PrimaryEntityName)

                     select a.Item4).FirstOrDefault();

                if (entityAction != null)
                {
                    localcontext.Trace(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} is firing for Entity: {1}, Message: {2}",
                        ChildClassName,
                        localcontext.PluginExecutionContext.PrimaryEntityName,
                        localcontext.PluginExecutionContext.MessageName));

                    entityAction.Invoke(localcontext);

                    // now exit - if the derived plug-in has incorrectly registered overlapping event registrations,
                    // guard against multiple executions.
                    return;
                }
            }
            catch (FaultException<OrganizationServiceFault> e)
            {
                localcontext.Trace(string.Format(CultureInfo.InvariantCulture, "Exception: {0}", e));
                throw;
            }
            catch (Exception ex)
            {
                localcontext.Trace(string.Format(CultureInfo.InvariantCulture, "Exception: {0}", ex));
                throw new InvalidPluginExecutionException("Error: " + (ex.InnerException != null ? ex.InnerException.Message : ex.Message), ex);
            }
            finally
            {
                localcontext.Trace(string.Format(CultureInfo.InvariantCulture, "Exiting {0}.Execute()", ChildClassName));
            }
        }
    }

    public static class PipelineStages
    {
        public const int PreValidation = 10;
        public const int PreOperation = 20;
        public const int PostOperation = 40;
    }

    public static class MessageNames
    {
        public const string Create = "Create";
        public const string Update = "Update";
        public const string Delete = "Delete";
        public const string SetState = "SetState";
        public const string SetStateDynamicEntity = "SetStateDynamicEntity";
        public const string Assign = "Assign";
        public const string Publish = "Publish";
        public const string PublishAll = "PublishAll";
        public const string Retrieve = "Retrieve";
        public const string RetrieveMultiple = "RetrieveMultiple";
        public const string Associate = "Associate";
        public const string Disassociate = "Disassociate";
    }
}
