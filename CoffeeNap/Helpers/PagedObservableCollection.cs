using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace CoffeeNap.Helpers;

/// <summary>Android's CollectionView adapter supports one native range insertion per page.</summary>
public sealed class PagedObservableCollection<T> : ObservableCollection<T>
{
    public PagedObservableCollection() { }
    public PagedObservableCollection(IEnumerable<T> items) : base(items) { }

    public void AddPage(IReadOnlyList<T> page)
    {
        if (page.Count == 0) return;
#if ANDROID
        CheckReentrancy();
        var start = Count;
        var added = page.ToArray();
        foreach (var item in added) Items.Add(item);
        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, added, start));
#else
        foreach (var item in page) Add(item);
#endif
    }
}
