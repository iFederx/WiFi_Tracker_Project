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
    /// This class contains a dictionary where elements are sorted under the insertion order
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
        /// 
        /// </summary>
        /// <param name="k"></param>
        /// <param name="v"></param>
        /// <param name="updater"></param>
        /// <returns></returns>
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
        public V pop(Func<V, K> keyExtractor)
        {
            locker.EnterWriteLock();
            V retval = popInternal(keyExtractor);
            locker.ExitWriteLock();
            return retval;
        }

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
