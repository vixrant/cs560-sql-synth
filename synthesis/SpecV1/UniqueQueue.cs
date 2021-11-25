using System.Collections.Generic;

namespace Rest560SpecV1
{
    public class UniqueQueue<T>
    {
        private HashSet<T> hashSet;
        private Queue<T> queue;
        public UniqueQueue()
        {
            hashSet = new HashSet<T>();
            queue = new Queue<T>();
        }
        public int Count
        {
            get
            {
                return hashSet.Count;
            }
        }
        public void Enqueue(T item)
        {
            if (hashSet.Add(item))
                queue.Enqueue(item);
        }
        public T Dequeue()
        {
            T item = queue.Dequeue();
            hashSet.Remove(item);
            return item;
        }
    }
}
