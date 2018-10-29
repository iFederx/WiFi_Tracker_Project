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
    class InsertionSortedConcurrentDictionary<K, V> 
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
        
        public V upsert(K k, V v, Func<V,V,V> updater)
        {
            LinkedNode<V> item;
            locker.EnterWriteLock();
            if (!mapper.TryGetValue(k, out item))
            {
                item = new LinkedNode<V>();
                item.value = v;
                item = mapper.GetOrAdd(k, item);
            }
            else
                item.value = updater(item.value, v);          
            if(first==null)
                first=item;
            if(last!=null)
                last.next=item;
            if (item.prec != null)
                item.prec.next = item.next;
            if (item.next != null)
                item.next.prec = item.prec;
            item.prec=last;
            last=item;
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
                v=default(V);
            return retval;
        }
        public V getNext(K k)
        {
            V retval;
            locker.EnterReadLock();
            if(k==null)
            {
                retval=first.value;
            }
            else
            {
                LinkedNode<V> n;
                bool knownKey=mapper.TryGetValue(k,out n);
                if(knownKey)
                    retval=n.next.value;
                else
                    retval=first.value;
            }
            locker.ExitReadLock();
            return retval;
        }
        public V getPrec(K k)
        {
            V retval;
            locker.EnterReadLock();
            if(k==null)
            {
                retval=last.value;
            }
            else
            {
                LinkedNode<V> n;
                bool knownKey=mapper.TryGetValue(k,out n);
                if(knownKey)
                    retval=n.prec.value;
                else
                    retval=last.value;
            }
            locker.ExitReadLock();
            return retval;
        }
        public bool removeExtreme(K k)
        {
            LinkedNode<V> n;
            locker.EnterWriteLock();
            bool retval=mapper.TryRemove(k,out n);
            retval=(n==first||n==last);
            if(retval)
            {
                if(n.prec==null)
                    first=n.next;
                else
                    n.prec.next=n.next;
                if(n.next==null)
                    last=n.prec;
                else
                    n.next.prec=n.prec;
                retval=true;
            }
            locker.ExitWriteLock();
            return retval;
        }
        

    }
}
