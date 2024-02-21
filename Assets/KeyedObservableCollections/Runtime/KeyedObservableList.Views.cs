namespace Hg.KeyedObservableCollections
{
    using System;
    using System.Collections.Generic;

    public partial class KeyedObservableList<TKey, TValue> : IList<TValue>, IReadOnlyList<TValue>, IKeyedObservableCollection<TKey, TValue>
        where TValue : IKeyed<TKey>
        where TKey : IEquatable<TKey>
    {
        IKeyedObservableCollectionView<TKey, TValue, TView> IKeyedObservableCollection<TKey, TValue>.CreateView<TView>(Func<TValue, TView> factory, bool reverse)
        {
            throw new NotImplementedException();
        }
    }
}
