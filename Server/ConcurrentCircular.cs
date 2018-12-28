using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class ConcurrentCircular<T>
    {
        UInt32 bitmask;
        UInt32 head = 0; //empty head
        Boolean wrapped = false;
        SpinLock sl = new SpinLock(false);
        T[] storage;
        public ConcurrentCircular(UInt32 dimension)
        {
            dimension--;
            dimension |= dimension >> 1;
            dimension |= dimension >> 2;
            dimension |= dimension >> 4;
            dimension |= dimension >> 8;
            dimension |= dimension >> 16;
            dimension++;
            storage = new T[dimension];
            bitmask = dimension - 1;
        }
        /// <summary>
        /// Returns a value at the i position in order from the head to the tail of the queue
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public T get(UInt32 i)
        {
            T retval;
            bool lockTaken = false;
            if (i > bitmask)
                retval = default(T);
            else
            {
                try
                {
                    sl.Enter(ref lockTaken);
                    i = (head + bitmask - i) & bitmask;
                    if (i >= head && !wrapped)
                        retval = default(T);
                    else
                        retval = storage[i];
                }
                finally
                {
                    if (lockTaken)
                        sl.Exit();
                }
            }
            return retval;
        }

        public void put(T value)
        {
            bool lockTaken = false;
            try
            {
                sl.Enter(ref lockTaken);
                storage[head] = value;
                head = (head + 1) & bitmask;
                wrapped = (head == 0) | wrapped;
            }
            finally
            {
                if (lockTaken)
                    sl.Exit();
            }
        }
    }
}