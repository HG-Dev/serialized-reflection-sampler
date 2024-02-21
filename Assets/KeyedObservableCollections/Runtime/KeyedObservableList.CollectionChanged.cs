namespace Hg.KeyedObservableCollections
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;

    public partial class KeyedObservableList<TKey, TValue> : IList<TValue>, IReadOnlyList<TValue>, IKeyedObservableCollection<TKey, TValue>
        where TValue : IKeyed<TKey>
        where TKey : IEquatable<TKey>
    {
        event NotifyCollectionChangedEventHandler INotifyCollectionChanged.CollectionChanged
        {
            add
            {
                _classicEventHandlers.Add(value);
                if (_classicEventHandlers.Count > 0)
                    this.KeyedCollectionChanged += NotifyClassicSubscribers;
            }
            remove
            {
                _classicEventHandlers.Remove(value);
                if (_classicEventHandlers.Count == 0)
                    this.KeyedCollectionChanged -= NotifyClassicSubscribers;
            }
        }

        private void NotifyClassicSubscribers(in NotifyCollectionChangedEventArgs<TValue> e)
        {
            var eventArgs = e.ToStandardEventArgs();
            foreach (var handler in _classicEventHandlers)
            {
                handler.Invoke(this, eventArgs);
            }
        }

        private readonly List<NotifyCollectionChangedEventHandler> _classicEventHandlers =
            new List<NotifyCollectionChangedEventHandler>();
    }
}
