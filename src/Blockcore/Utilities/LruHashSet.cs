using System.Collections.Generic;

namespace Blockcore.Utilities
{
    public class LruHashSet<T>
    {
        private readonly LinkedList<T> lru;

        private HashSet<T> items;

        private long maxSize;

        private long itemCount;

        private readonly object lockObject = new object();

        public LruHashSet(long maxSize = long.MaxValue)
        {
            this.lru = new LinkedList<T>();
            this.items = new HashSet<T>();

            this.maxSize = maxSize;
            this.itemCount = 0;
        }

        public void AddOrUpdate(T item)
        {
            lock (this.lockObject)
            {
                // First check if we are performing the 'Update' case. No change to item count.
                if (this.items.Contains(item))
                {
                    this.lru.Remove(item);
                    this.lru.AddLast(item);

                    return;
                }

                // Otherwise it's 'Add'.
                // First perform the size test.
                if ((this.itemCount + 1) > this.maxSize)
                {
                    LinkedListNode<T> tempItem = this.lru.First;
                    this.lru.RemoveFirst();
                    this.items.Remove(tempItem.Value);
                    this.itemCount--;
                }

                this.lru.AddLast(item);
                this.items.Add(item);
                this.itemCount++;
            }
        }

        public void Clear()
        {
            lock (this.lockObject)
            {
                this.lru.Clear();
                this.items.Clear();
                this.itemCount = 0;
            }
        }

        public bool Contains(T item)
        {
            lock (this.lockObject)
            {
                // Fastest to check the hashmap.
                return this.items.Contains(item);
            }
        }

        public void Remove(T item)
        {
            lock (this.lockObject)
            {
                this.lru.Remove(item);
                this.items.Remove(item);
                this.itemCount--;
            }
        }
    }
}