using EventBus.Base.Abstraction;
using EventBus.Base.SubManager;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.Base.Events
{
    public class BaseEventBus : IEventBus
    {
        public readonly IServiceProvider ServiceProvider;
        public readonly IEventBusSubscriptionManager SubsManager;

        private EventBusConfig eventBusConfig;

        public BaseEventBus(EventBusConfig config, IServiceProvider serviceProvider)
        {
            eventBusConfig= config;
            this.ServiceProvider = serviceProvider;
            SubsManager = new InMemoryEventBusSubscriptionManager(ProccessEventName);

        }

        public virtual string ProccessEventName(string eventName)
        {
            if (eventBusConfig.DeleteEventPrefix)
            {
                eventName = eventName.TrimStart(eventBusConfig.EventNamePrefix.ToArray());
            }
            if (eventBusConfig.DeleteEventSuffix)
            {
                eventName = eventName.TrimStart(eventBusConfig.EventNameSuffix.ToArray());
            }

            return eventName;
        }

        public virtual string GetSubName(string eventName)
        {
            return $"{eventBusConfig.SubScriberClientAppName}.{ProccessEventName(eventName)}";
        }

        public virtual void Dispose()
        {
            eventBusConfig = null;
        }

        public async Task<bool> ProcessEvent(string eventName, string message)
        {
            eventName= ProccessEventName(eventName);
            var proccesed = false;

            if (SubsManager.HasSubscriptionsForEvent(eventName))
            {
                var subScriptions = SubsManager.GetHandlersForEvent(eventName);
                using (var scope = ServiceProvider.CreateScope())
                {
                    foreach (var item in subScriptions)
                    {
                        // NugetPacket den ServiceProvider için Microsoft.Extensions.DependencyInjection eklendi
                        var handler = ServiceProvider.GetService(item.HandlerType);
                        if (handler == null) continue;
                        
                        var eventType = SubsManager.GetEventTypeName($"{eventBusConfig.EventNamePrefix}{eventName}{eventBusConfig.EventNameSuffix}");
                        var integrationEvent = JsonConvert.DeserializeObject(message, eventType);   
                        var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                        await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { integrationEvent }); //reflection kullanıldı
                    }
                }

                proccesed= true;
            }
            return proccesed; 
        }

        public void Publish(IntegrationEvent @event)
        {
            throw new NotImplementedException();
        }

        public void Subscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            throw new NotImplementedException();
        }

        public void UnSubscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            throw new NotImplementedException();
        }
    }
}
