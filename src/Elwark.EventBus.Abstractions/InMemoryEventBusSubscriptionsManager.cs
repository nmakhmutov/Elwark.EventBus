﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Elwark.EventBus.Abstractions
{
    public class InMemoryEventBusSubscriptionsManager : IEventBusSubscriptionsManager
    {
        private readonly Dictionary<string, List<SubscriptionInfo>> _handlers;

        private readonly HashSet<Type> _eventTypes;

        public event EventHandler<string> OnEventRemoved;

        public InMemoryEventBusSubscriptionsManager()
        {
            _handlers = new Dictionary<string, List<SubscriptionInfo>>();
            _eventTypes = new HashSet<Type>();
        }

        public bool IsEmpty => !_handlers.Keys.Any();

        public void Clear() => _handlers.Clear();

        public void AddDynamicSubscription<TH>(string eventName)
            where TH : IDynamicIntegrationEventHandler
        {
            DoAddSubscription(typeof(TH), eventName, true);
        }

        public void AddSubscription<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = GetEventKey<T>();
            DoAddSubscription(typeof(TH), eventName, false);
            _eventTypes.Add(typeof(T));
        }

        private void DoAddSubscription(Type handlerType, string eventName, bool isDynamic)
        {
            if (!HasSubscriptionsForEvent(eventName))
                _handlers.Add(eventName, new List<SubscriptionInfo>());

            if (_handlers[eventName].Any(s => s.HandlerType == handlerType))
                throw new ArgumentException(
                    $"Handler Type {handlerType.Name} already registered for '{eventName}'", nameof(handlerType));

            _handlers[eventName]
                .Add(isDynamic
                    ? SubscriptionInfo.Dynamic(handlerType)
                    : SubscriptionInfo.Typed(handlerType)
                );
        }


        public void RemoveDynamicSubscription<TH>(string eventName)
            where TH : IDynamicIntegrationEventHandler
        {
            var handlerToRemove = FindDynamicSubscriptionToRemove<TH>(eventName);
            DoRemoveHandler(eventName, handlerToRemove);
        }


        public void RemoveSubscription<T, TH>()
            where TH : IIntegrationEventHandler<T>
            where T : IntegrationEvent
        {
            var handlerToRemove = FindSubscriptionToRemove<T, TH>();
            var eventName = GetEventKey<T>();
            DoRemoveHandler(eventName, handlerToRemove);
        }


        private void DoRemoveHandler(string eventName, SubscriptionInfo subsToRemove)
        {
            if (subsToRemove is null)
                return;

            _handlers[eventName].Remove(subsToRemove);

            if (_handlers[eventName].Any())
                return;

            _handlers.Remove(eventName);

            var eventType = _eventTypes.SingleOrDefault(e => e.Name == eventName);

            if (eventType != null)
                _eventTypes.Remove(eventType);

            RaiseOnEventRemoved(eventName);
        }

        public IEnumerable<SubscriptionInfo> GetHandlersForEvent<T>() where T : IntegrationEvent =>
            GetHandlersForEvent(GetEventKey<T>());

        public IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName) =>
            _handlers[eventName];

        private void RaiseOnEventRemoved(string eventName) =>
            OnEventRemoved?.Invoke(this, eventName);

        private SubscriptionInfo FindDynamicSubscriptionToRemove<TH>(string eventName)
            where TH : IDynamicIntegrationEventHandler =>
            DoFindSubscriptionToRemove(eventName, typeof(TH));


        private SubscriptionInfo FindSubscriptionToRemove<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T> =>
            DoFindSubscriptionToRemove(GetEventKey<T>(), typeof(TH));

        private SubscriptionInfo DoFindSubscriptionToRemove(string eventName, Type handlerType) =>
            HasSubscriptionsForEvent(eventName)
                ? _handlers[eventName].SingleOrDefault(s => s.HandlerType == handlerType)
                : null;

        public bool HasSubscriptionsForEvent<T>() where T : IntegrationEvent =>
            HasSubscriptionsForEvent(GetEventKey<T>());

        public bool HasSubscriptionsForEvent(string eventName) =>
            _handlers.ContainsKey(eventName);

        public Type GetEventTypeByName(string eventName) =>
            _eventTypes.SingleOrDefault(t => t.Name == eventName);

        public string GetEventKey<T>() =>
            typeof(T).Name;
    }
}