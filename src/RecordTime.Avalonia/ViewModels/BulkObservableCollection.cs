using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace RecordTime.Avalonia.ViewModels;

public sealed class BulkObservableCollection<T> : ObservableCollection<T>
{
    private bool _suppressNotifications;

    public IDisposable DeferNotifications()
    {
        _suppressNotifications = true;
        return new NotificationScope(this);
    }

    public void ReplaceWith(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        using (DeferNotifications())
        {
            Clear();
            foreach (var item in items)
            {
                Add(item);
            }
        }
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (_suppressNotifications)
        {
            return;
        }

        base.OnCollectionChanged(e);
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (_suppressNotifications)
        {
            return;
        }

        base.OnPropertyChanged(e);
    }

    private sealed class NotificationScope : IDisposable
    {
        private readonly BulkObservableCollection<T>? _owner;
        private bool _disposed;

        public NotificationScope(BulkObservableCollection<T> owner)
        {
            _owner = owner;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_owner == null)
            {
                return;
            }

            _owner._suppressNotifications = false;
            _owner.OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            _owner.OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            _owner.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}

