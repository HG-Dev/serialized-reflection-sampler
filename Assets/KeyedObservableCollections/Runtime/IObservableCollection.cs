using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Hg.KeyedObservableCollections
{
    public delegate void NotifyCollectionChangedEventHandler<T>(in NotifyCollectionChangedEventArgs<T> e);

    /// <summary>
    /// Implemented by classes which maintain a unique ID.
    /// </summary>
    /// <typeparam name="TKey">The type of this object's ID.</typeparam>
    // ReSharper disable once TypeParameterCanBeVariant
    public interface IKeyed<TKey> where TKey : IEquatable<TKey>
    {
        TKey Id { get; }
    }

    /// <summary>
    /// Implemented by collections that can be indexed both by an ID and collection index.
    /// </summary>
    /// <typeparam name="TKey">The type of indexed ID.</typeparam>
    /// <typeparam name="TValue">A class which can be identified by a key of TKey type that it holds.</typeparam>
    public interface IKeyedObservableCollection<TKey, TValue>
        : IReadOnlyDictionary<TKey, TValue>, IReadOnlyList<TValue>, INotifyCollectionChanged
        where TValue : IKeyed<TKey>         // TKeyed: Base data with an ID
        where TKey : IEquatable<TKey>   // TKeyType: The type of the ID (string, int, otherwise)
    {
        event NotifyCollectionChangedEventHandler<TValue> KeyedCollectionChanged;

        object SyncRoot { get; }

        IKeyedObservableCollectionView<TKey, TValue, TView> CreateView<TView>(Func<TValue, TView> factory, bool reverse = false);
    }

    /// <summary>
    /// Implemented by a collection of views that observe an IKeyedObservableCollection ItemsSource.
    /// At this level, filters can be applied to affect resulting views
    /// </summary>
    /// <typeparam name="TKey">The type of indexed ID.</typeparam>
    /// <typeparam name="TValue">A class which can be identified by a key of TKeyType that it holds.</typeparam>
    /// <typeparam name="TView">A class used to view TKeyed items.</typeparam>
    public interface IKeyedObservableCollectionView<TKey, TValue, TView>
        : IReadOnlyDictionary<TKey, TView>, IReadOnlyList<TView>, INotifyCollectionChanged, IDisposable
        where TKey : IEquatable<TKey>
        where TValue : IKeyed<TKey>
    {
        IKeyedObservableCollection<TKey, TValue> ItemsSource { get; }
        object SyncRoot => ItemsSource.SyncRoot;
        event NotifyCollectionChangedEventHandler<TView> KeyedCollectionChanged;

        // event NotifyCollectionChangedEventHandler<T> RoutingCollectionChanged;

        //void AttachFilter(ISynchronizedViewFilter<T, TView> filter);
        //void ResetFilter(Action<T, TView> resetAction);
        //INotifyCollectionChangedSynchronizedView<T, TView> WithINotifyCollectionChanged();
    }
}
