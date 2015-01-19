using System;

namespace Clockwork
{
    /// <summary>
    /// A pool of integers, storing each pooled value as a single bit.
    /// </summary>
    public class IntegerPool
    {
        private uint[] words;

        public int Count { get; private set; }

        /// <summary>
        /// Creates a new <see cref="IntegerPool" /> instance.
        /// </summary>
        /// <param name="capacity">The number of pooled intager values.</param>
        public IntegerPool(int capacity)
        {
            words = new uint[(capacity - 1) / 32 + 1];
            for (int i = 0; i < words.Length; i++)
                words[i] = 0xFFFFFFFF;

            words[words.Length - 1] >>= (32 - capacity % 32);

            Count = capacity;
        }

        /// <summary>
        /// Removes a value from the pool and returns it.
        /// </summary>
        /// <returns>The acquired value.</returns>
        public int Acquire()
        {
            for (int wordIndex = 0; wordIndex < words.Length; wordIndex++)
            {
                uint word = words[wordIndex];
                if (word == 0)
                    continue;

                int bitIndex = GetTrailingZeros(word);
                words[wordIndex] &= ~(1U << bitIndex);
                Count--;
                return bitIndex + wordIndex * 32;
            }

            throw new InvalidOperationException("Pool is empty");
        }

        /// <summary>
        /// Returns a value to the pool.
        /// </summary>
        /// <param name="value">The value to return.</param>
        public void Release(int value)
        {
            int wordIndex = value / 32;
            int bitIndex = value % 32;
            var mask = (1U << bitIndex);

            if ((words[wordIndex] & mask) != 0)
                throw new IndexOutOfRangeException(string.Format("Pool already contains entry {0}", value));

            words[wordIndex] |= mask;

            Count++;
        }

        // http://graphics.stanford.edu/~seander/bithacks.html
        private static int GetTrailingZeros(uint word)
        {
            if ((word & 0x1) != 0)
            {
                return 0;
            }
            else
            {
                int result = 1;
                if ((word & 0xFFFF) == 0)
                {
                    word >>= 16;
                    result += 16;
                }
                if ((word & 0xFF) == 0)
                {
                    word >>= 8;
                    result += 8;
                }
                if ((word & 0xF) == 0)
                {
                    word >>= 4;
                    result += 4;
                }
                if ((word & 0x3) == 0)
                {
                    word >>= 2;
                    result += 2;
                }
                result -= (int)(word & 0x1);

                return result;
            }
        }
    }
}
