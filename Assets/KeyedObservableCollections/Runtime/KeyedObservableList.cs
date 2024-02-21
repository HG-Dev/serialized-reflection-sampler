using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
// ReSharper disable PossibleMultipleEnumeration

// Similar to https://github.com/microsoft/referencesource/blob/master/mscorlib/system/collections/objectmodel/keyedcollection.cs
namespace Hg.KeyedObservableCollections
{
    using Internal;
    using System.Collections.Specialized;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// An observable collection that can be indexed both via list index and an arbitrary embedded key found in TValue.
    /// </summary>
    /// <typeparam name="TKey">The type of key found in TValue.</typeparam>
    /// <typeparam name="TValue">An item with embedded key for identification.</typeparam>
    public partial class KeyedObservableList<TKey, TValue> : IList<TValue>, IReadOnlyList<TValue>, IKeyedObservableCollection<TKey, TValue>
        where TValue : IKeyed<TKey>
        where TKey : IEquatable<TKey>
    {
        private readonly List<TValue> list;
        private readonly Dictionary<TKey, TValue> dict;
        private IEnumerable<TKey> _keys;
        private IEnumerable<TValue> _values;

        private const string AddOrFindNullMessage = "Null items cannot be indexed";
        private const string IdAlreadyExistsMessage = "Item with ID {0} already exists in list";
        private const string NullOrEmptyRangeMessage = "Items argument is null or empty";

        public object SyncRoot { get; } = new object();



        public KeyedObservableList()
        {
            list = new List<TValue>();
            dict = new Dictionary<TKey, TValue>();
        }

        public KeyedObservableList(int capacity)
        {
            list = new List<TValue>(capacity);
            dict = new Dictionary<TKey, TValue>(capacity);
        }

        public KeyedObservableList(IEnumerable<TValue> collection)
        {
            list = new List<TValue>();
            dict = new Dictionary<TKey, TValue>();

            foreach (var item in collection)
            {
                list.Add(item);
                dict.Add(item.Id, item);
            }
        }

        bool IReadOnlyDictionary<TKey, TValue>.ContainsKey(TKey key)
        {
            throw new NotImplementedException();
        }

        bool IReadOnlyDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value)
        {
            throw new NotImplementedException();
        }

        public TValue this[TKey key]
        {
            get
            {
                lock (SyncRoot)
                {
                    return dict[key];
                }
            }
            set
            {
                lock (SyncRoot)
                {
                    if (dict.TryAdd(key, value))
                    {
                        list.Add(value);
                    }
                    else
                    {
                        var index = list.FindIndex(item => item.Id.Equals(key));
                        var oldValue = list[index];
                        list[index] = value;
                        dict[key] = value;
                        KeyedCollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<TValue>.Replace(value, oldValue, index));
                    }
                }
            }
        }

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => _keys;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => _values;

        public TValue this[int index]
        {
            get
            {
                lock (SyncRoot)
                {
                    return list[index];
                }
            }
            set
            {
                lock (SyncRoot)
                {
                    var oldValue = list[index];
                    list[index] = value;
                    KeyedCollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<TValue>.Replace(value, oldValue, index));
                }
            }
        }

        public int Count
        {
            get
            {
                lock (SyncRoot)
                {
                    return list.Count;
                }
            }
        }

        public bool IsReadOnly => false;

        /// <summary>
        /// Allows for observation of this list with less boxing than CollectionChanged.
        /// </summary>
        public event NotifyCollectionChangedEventHandler<TValue> KeyedCollectionChanged;

        /// <summary>
        /// Append a single item to the end of the list.
        /// </summary>
        public void Add([NotNull] TValue item)
        {
            if (item is null)
                throw new ArgumentNullException(nameof(item), AddOrFindNullMessage);
            if (dict.ContainsKey(item.Id))
                throw new ArgumentException($"Item with ID {item.Id} already exists in list");

            lock (SyncRoot)
            {
                var index = list.Count;
                list.Add(item);
                dict.Add(item.Id, item);
                KeyedCollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<TValue>.Add(item, index));
            }
        }

        /// <summary>
        /// Enumerate and append items to the end of the list.
        /// </summary>
        public void AddRange(IEnumerable<TValue> items)
        {
            if (items is null)
                throw new ArgumentNullException(nameof(items), AddOrFindNullMessage);
            if (!items.Any())
                throw new ArgumentException(NullOrEmptyRangeMessage);

            // Copy to avoid repeated iteration
            using var ccItems = new CloneCollection<TValue>(items);
            foreach (var item in ccItems.Span)
            {
                if (item is null)
                    throw new ArgumentNullException(nameof(items), AddOrFindNullMessage);
                if (dict.ContainsKey(item.Id))
                    throw new ArgumentException(nameof(IKeyed<TKey>.Id), string.Format(IdAlreadyExistsMessage, item.Id));
            }

            lock (SyncRoot)
            {
                var index = list.Count;

                list.AddRange(ccItems.AsEnumerable());
                foreach (var item in ccItems.Span)
                    dict.Add(item.Id, item);

                KeyedCollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<TValue>.Add(ccItems.Span, index));
            }
        }

        /// <summary>
        /// Append an array of items to the end of the list.
        /// </summary>
        public void AddRange(TValue[] items)
        {
            if (items is null)
                throw new ArgumentNullException(nameof(items), AddOrFindNullMessage);
            if (items.Length == 0)
                throw new ArgumentException(NullOrEmptyRangeMessage);

            foreach (var item in items)
            {
                if (item is null)
                    throw new ArgumentNullException(nameof(items), AddOrFindNullMessage);
                if (dict.ContainsKey(item.Id))
                    throw new ArgumentException(nameof(IKeyed<TKey>.Id), string.Format(IdAlreadyExistsMessage, item.Id));
            }

            lock (SyncRoot)
            {
                var index = list.Count;
                list.AddRange(items);
                foreach (var item in items)
                    dict.Add(item.Id, item);
                KeyedCollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<TValue>.Add(items, index));
            }
        }

        /// <summary>
        /// Append a span of items to the end of the list.
        /// </summary>
        public void AddRange(ReadOnlySpan<TValue> items)
        {
            if (items.IsEmpty)
                throw new ArgumentException(NullOrEmptyRangeMessage);

            foreach (var item in items)
            {
                if (item is null)
                    throw new ArgumentNullException(nameof(items), AddOrFindNullMessage);
                if (dict.ContainsKey(item.Id))
                    throw new ArgumentException(nameof(IKeyed<TKey>.Id), string.Format(IdAlreadyExistsMessage, item.Id));
            }

            lock (SyncRoot)
            {
                var index = list.Count;
                foreach (var item in items)
                {
                    list.Add(item);
                    dict.Add(item.Id, item);
                }

                KeyedCollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<TValue>.Add(items, index));
            }
        }

        /// <inheritdoc/>
        public void Clear()
        {
            lock (SyncRoot)
            {
                list.Clear();
                dict.Clear();
                KeyedCollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<TValue>.Reset());
            }
        }

        /// <inheritdoc/>
        public bool Contains([NotNull] TValue item)
        {
            if (item is null)
                throw new ArgumentNullException();

            lock (SyncRoot)
            {
                return item is not null && dict.ContainsKey(item.Id);
            }
        }

        /// <inheritdoc/>
        public void CopyTo([NotNull] TValue[] array, int arrayIndex)
        {
            lock (SyncRoot)
            {
                list.CopyTo(array, arrayIndex);
            }
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            lock (SyncRoot)
            {
                foreach (var item in list)
                {
                    yield return new KeyValuePair<TKey, TValue>(item.Id, item);
                }
            }
        }

        /// <inheritdoc/>
        public IEnumerator<TValue> GetEnumerator()
        {
            lock (SyncRoot)
            {
                foreach (var item in list)
                {
                    yield return item;
                }
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Find the index (int) of an item in the list.
        /// </summary>
        public int IndexOf([NotNull] TValue item)
        {
            if (item is null)
                throw new ArgumentNullException(nameof(item), AddOrFindNullMessage);

            lock (SyncRoot)
            {
                if (dict.ContainsKey(item.Id))
                    return list.FindIndex(keyed => keyed.Id.Equals(item.Id));
                return -1;
            }
        }

        /// <summary>
        /// Insert a single item into the list at a given index.
        /// </summary>
        public void Insert(int index, [NotNull] TValue item)
        {
            if (item is null)
                throw new ArgumentNullException(nameof(item), AddOrFindNullMessage);

            if (dict.ContainsKey(item.Id))
                throw new ArgumentException(nameof(item), string.Format(IdAlreadyExistsMessage, item.Id));

            lock (SyncRoot)
            {
                list.Insert(index, item);
                dict.Add(item.Id, item);

                KeyedCollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<TValue>.Add(item, index));
            }
        }

        /// <summary>
        /// Copy an array of items into the list from a given index.
        /// </summary>
        public void InsertRange(int index, [NotNull] TValue[] items)
        {
            if (items.Length == 0)
                throw new ArgumentException(NullOrEmptyRangeMessage);

            foreach (var item in items)
            {
                if (item is null)
                    throw new ArgumentNullException(nameof(items), AddOrFindNullMessage);
                if (dict.ContainsKey(item.Id))
                    throw new ArgumentException(nameof(IKeyed<TKey>.Id), string.Format(IdAlreadyExistsMessage, item.Id));
            }

            lock (SyncRoot)
            {
                list.InsertRange(index, items);
                foreach (var item in items)
                    dict.Add(item.Id, item);
                KeyedCollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<TValue>.Add(items, index));
            }
        }

        /// <summary>
        /// Enumerate and copy items into the list from a given index.
        /// </summary>
        public void InsertRange(int index, [NotNull] IEnumerable<TValue> items)
        {
            if (items is null || !items.Any())
                throw new ArgumentException(NullOrEmptyRangeMessage);

            using var ccItems = new CloneCollection<TValue>(items);
            foreach (var item in ccItems.Span)
            {
                if (item is null)
                    throw new ArgumentNullException(nameof(items), AddOrFindNullMessage);
                if (dict.ContainsKey(item.Id))
                    throw new ArgumentException(nameof(IKeyed<TKey>.Id), string.Format(IdAlreadyExistsMessage, item.Id));
            }

            lock (SyncRoot)
            {

                foreach (var item in ccItems.Span)
                {
                    list.Add(item);
                    dict.Add(item.Id, item);
                }

                KeyedCollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<TValue>.Add(ccItems.Span, index));
            }
        }

        /// <summary>
        /// Copy a span of items into the list from a given index.
        /// </summary>
        public void InsertRange(int index, ReadOnlySpan<TValue> items)
        {
            if (items.IsEmpty)
                throw new ArgumentException(NullOrEmptyRangeMessage);

            foreach (var item in items)
            {
                if (item is null)
                    throw new ArgumentNullException(nameof(items), AddOrFindNullMessage);
                if (dict.ContainsKey(item.Id))
                    throw new ArgumentException(nameof(IKeyed<TKey>.Id), string.Format(IdAlreadyExistsMessage, item.Id));
            }

            lock (SyncRoot)
            {
                foreach (var item in items)
                {
                    list.Add(item);
                    dict.Add(item.Id, item);
                }

                KeyedCollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<TValue>.Add(items, index));
            }
        }

        /// <summary>
        /// Remove a single item from the list.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>False if the item was not found in the list.</returns>
        public bool Remove([NotNull] TValue item)
        {
            if (item is null)
                throw new ArgumentNullException(AddOrFindNullMessage);

            lock (SyncRoot)
            {
                if (!dict.ContainsKey(item.Id))
                    return false;

                var index = list.IndexOf(item);

                if (index >= 0)
                {
                    list.RemoveAt(index);
                    dict.Remove(item.Id);
                    KeyedCollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<TValue>.Remove(item, index));
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Remove a single item at a given index
        /// </summary>
        public void RemoveAt(int index)
        {
            lock (SyncRoot)
            {
                var item = list[index];
                dict.Remove(item.Id);
                list.RemoveAt(index);
                KeyedCollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<TValue>.Remove(item, index));
            }
        }

        /// <summary>
        /// Remove a section of the list starting at index
        /// </summary>
        /// <param name="index">Start of removed range</param>
        /// <param name="count">Number of items in removed range</param>
        public void RemoveRange(int index, int count)
        {
            lock (SyncRoot)
            {
#if NET5_0_OR_GREATER
                var range = CollectionsMarshal.AsSpan(list).Slice(index, count);
#else
                var range = list.GetRange(index, count);
#endif

                // Copy before removing for event handler
                using var ccItems = new CloneCollection<TValue>(range);
                list.RemoveRange(index, count);
                foreach (var item in ccItems.Span)
                    dict.Remove(item.Id);
                KeyedCollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<TValue>.Remove(ccItems.Span, index));
            }
        }

        /// <summary>
        /// Move an item in the list from one index to another.
        /// The key dictionary is not affected.
        /// </summary>
        /// <param name="oldIndex">Previous index of item</param>
        /// <param name="newIndex">New index of item after removal is executed</param>
        public void Move(int oldIndex, int newIndex)
        {
            if (oldIndex == newIndex)
                return;

            lock (SyncRoot)
            {
                var removedItem = list[oldIndex];
                list.RemoveAt(oldIndex);
                list.Insert(newIndex, removedItem);
                KeyedCollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<TValue>.Move(removedItem, newIndex, oldIndex));
            }
        }

        /// <summary>
        /// Use the ID found on item to overwrite a value with the same ID in the list
        /// </summary>
        public void Update([NotNull] TValue item)
        {
            if (item is null)
                throw new ArgumentNullException(AddOrFindNullMessage);
            if (!dict.ContainsKey(item.Id))
                throw new ArgumentOutOfRangeException(nameof(IKeyed<TKey>.Id));

            this[item.Id] = item;
        }
    }
}
