using System;

namespace Lucky.HomeMock.Core
{
    public class ItemEventArgs<T> : EventArgs
    {
        public T Item { get; private set; }

        public ItemEventArgs(T item)
        {
            Item = item;
        }
    }
}