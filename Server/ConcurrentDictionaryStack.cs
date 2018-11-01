using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    /// <summary>
    /// This class contains a dictionary where elements are sorted under the insertion and update order in FIFO fashion
    /// </summary>
    /// <typeparam name="K"></typeparam>
    /// <typeparam name="V"></typeparam>
    class ConcurrentDictionaryStack<K, V> where V:class
    {
        class LinkedNode<T>
        {
            internal LinkedNode<T> next=null;
            internal LinkedNode<T> prec=null;//if null, is first
            internal T value;
        }
        LinkedNode<V> first = null;
        LinkedNode<V> last = null;
        ConcurrentDictionary<K, LinkedNode<V>> mapper = new ConcurrentDictionary<K, LinkedNode<V>>();
        ReaderWriterLockSlim locker = new ReaderWriterLockSlim();
        /// <summary>
        /// Insert or update the value for the given key
        /// </summary>
        /// <param name="k">Key</param>
        /// <param name="v">Value</param>
        /// <param name="updater">Function to call to update the value if it already exists in the map. It is call as updater(existingValue, givenValue)</param>
        /// <returns>The final value inserted or updated</returns>
        public V upsert(K k, V v, Func<V,V,V> updater)
        {
            LinkedNode<V> item;
            locker.EnterWriteLock();
            if (!mapper.TryGetValue(k, out item))
            {
                item = new LinkedNode<V>();
                item.value = v;
                item.next = null;
                item.prec = null;
                item = mapper.GetOrAdd(k, item);
            }
            else
               item.value=updater(item.value, v);

            if (first == null)
                first = item;
            else if (item.prec == null&&item.next!=null)
                first = item.next;
            
            if (item.prec != null)
                item.prec.next = item.next;
            if (item.next != null)
                item.next.prec = item.prec;
            if (last != item)
                item.prec = last;
            if (last != null && last!=item)
                last.next = item;
            last = item;
            item.next = null;
            
            locker.ExitWriteLock();
            return item.value;
        }
        public bool getKey(K k, out V v)
        {
            LinkedNode<V> n;
            bool retval=mapper.TryGetValue(k,out n);
            if(retval)
                v=n.value;
            else
                v=null;
            return retval;
        }
        /// <summary>
        /// Return the value at the tail of the queue (the oldest inserted or updated) without removing it
        /// </summary>
        /// <returns></returns>
        public V peek()
        {
            V retval;
            locker.EnterReadLock();
            if (first != null)
                retval = first.value;
            else
                retval = null;
            locker.ExitReadLock();
            return retval;
        }
        public V remove(K k)
        {
            locker.EnterWriteLock();
            LinkedNode<V> n;
            V retval = null;
            if(mapper.TryGetValue(k,out n))
            {
                mapper.TryRemove(k, out n);
                retval = n.value;
                if (n.prec == null)
                    first = n.next;
                else
                    n.prec.next = n.next;
                if (n.next == null)
                    last = n.prec;
                else
                    n.next.prec = n.prec;
            }
            locker.ExitWriteLock();
            return retval;
        }
        private V popInternal(Func<V, K> keyExtractor)
        {
            LinkedNode<V> n = first;
            if (n.prec == null)
                first = n.next;
            else
                n.prec.next = n.next;
            if (n.next == null)
                last = n.prec;
            else
                n.next.prec = n.prec;
            mapper.TryRemove(keyExtractor(n.value),out n);
            return n.value;
        }
        /// <summary>
        /// Return the value at the tail of the queue (the oldest inserted or updated) and remove it
        /// </summary>
        /// <param name="keyExtractor">Function that, given the value stored in the structure, returns the key under which is saved</param>
        /// <returns></returns>
        public V pop(Func<V, K> keyExtractor)
        {
            locker.EnterWriteLock();
            V retval = popInternal(keyExtractor);
            locker.ExitWriteLock();
            return retval;
        }
        /// <summary>
        /// Peeks the value at the tail of the queue (the oldest inserted or updated) and removes it if a function applied to the value returns a true condition.
        /// </summary>
        /// <param name="condition">Function that, given the value, returns if must be removed or not</param>
        /// <param name="keyExtractor">Function that, given the value stored in the structure, returns the key under which is saved</param>
        /// <param name="peekedValue">Value peeked</param>
        /// <returns>If the item has been removed</returns>
        public bool popConditional(Func<V,Boolean> condition,Func<V,K> keyExtractor,out V peekedValue)
        {
            bool removed=false;
            locker.EnterWriteLock();
            if (first != null)
            {
                peekedValue = first.value;
                if (condition(peekedValue))
                {
                    popInternal(keyExtractor);
                    removed = true;
                }
            }
            else
                peekedValue = null;
            locker.ExitWriteLock();
            return removed;
        }
        public ICollection<V> getAll()
        {
            List<V> allElements = new List<V>();
            foreach (LinkedNode<V> val in mapper.Values)
                allElements.Add(val.value);
            return allElements;
        }
        

    }
}
