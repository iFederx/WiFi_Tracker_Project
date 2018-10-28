using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            LinkedNode<T> next;
            LinkedNode<T> prec;//if null, is first
            T value;
        }
        LinkedNode<V> first = null;
        LinkedNode<V> last = null;
        ConcurrentDictionary<K, LinkedNode<V>> mapper = new ConcurrentDictionary<K, LinkedNode<V>>();
        
        public void update(K k, V v)
        {

        }
        public bool getKey(K k, out V v)
        {
        }
        public V getByMin(ulong offset)
        {

        }
        public V getByMax(ulong offset)
        {

        }
        public bool remove(K k)
        {

        }
        

    }
}
