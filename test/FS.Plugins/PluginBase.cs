using System;
using Microsoft.Xrm.Sdk;

namespace FS.Plugins
{
    public interface ILocalPluginContext
    {
        IServiceProvider ServiceProvider { get; }
        IPluginExecutionContext PluginExecutionContext { get; }
        IOrganizationService OrganizationService { get; }
        IOrganizationService InitiatingUserOrganizationService { get; }
        ITracingService TracingService { get; }
        void Trace(string message);
    }

    /// <summary>
    /// Base class for all Dataverse plugins. Provides a strongly-typed
    /// local context with tracing, service factory, and execution context.
    /// </summary>
    public abstract class PluginBase : IPlugin
    {
        protected string ChildClassName { get; }

        /// <param name="childClassName">Pass typeof(YourPluginClass) from the derived constructor.</param>
        protected PluginBase(Type childClassName)
        {
            ChildClassName = childClassName?.ToString() ?? throw new ArgumentNullException(nameof(childClassName));
        }

        public void Execute(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));

            var context = new LocalPluginContext(serviceProvider);
            context.Trace($"Entered {ChildClassName}");

            try
            {
                ExecuteDataversePlugin(context);
            }
            catch (InvalidPluginExecutionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                context.Trace($"Unhandled exception: {ex}");
                throw new InvalidPluginExecutionException(
                    $"Unhandled exception in {ChildClassName}: {ex.Message}", ex);
            }
            finally
            {
                context.Trace($"Exiting {ChildClassName}");
            }
        }

        protected abstract void ExecuteDataversePlugin(ILocalPluginContext localPluginContext);
    }

    internal class LocalPluginContext : ILocalPluginContext
    {
        public IServiceProvider ServiceProvider { get; }
        public IOrganizationServiceFactory ServiceFactory { get; }
        public IOrganizationService OrganizationService { get; }
        public IOrganizationService InitiatingUserOrganizationService { get; }
        public IPluginExecutionContext PluginExecutionContext { get; }
        public ITracingService TracingService { get; }

        public LocalPluginContext(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            TracingService = serviceProvider.GetService<ITracingService>();
            PluginExecutionContext = serviceProvider.GetService<IPluginExecutionContext>();
            ServiceFactory = serviceProvider.GetService<IOrganizationServiceFactory>();
            OrganizationService = ServiceFactory.CreateOrganizationService(PluginExecutionContext.UserId);
            InitiatingUserOrganizationService = ServiceFactory.CreateOrganizationService(PluginExecutionContext.InitiatingUserId);
        }

        public void Trace(string message) => TracingService.Trace(message);
    }
}
