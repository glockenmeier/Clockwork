using System;
using System.Collections.Generic;
using SiliconStudio.Core;
using SiliconStudio.Core.Collections;
using SiliconStudio.Core.Serialization.Serializers;

namespace Clockwork.Data
{
    [DataContract(Inherited = true)]
    [DataSerializer(typeof(DataObjectSerializer<DataObject>))]
    public class DataObject
    {
        private static readonly object UnsetValue = new object();

        private static readonly object RemovedValue = new object();

        private FastListStruct<ValueEntry> values = new FastListStruct<ValueEntry>(0);
        private int modifiedValueCount;

        public static readonly DataProperty<DataObject> InheritanceParentPropertyKey = new DataProperty<DataObject>("InheritanceParentPropertyKey", typeof(DataObject), 0);
        public DataObject InheritanceParent
        {
            get { return Get(InheritanceParentPropertyKey); }
            set { Set(InheritanceParentPropertyKey, value); }
        }

        public T Get<T>(DataProperty<T> key)
        {
            int index;
            if (GetKeyIndex(key, out index))
            {
                var entry = values[index];

                if (entry.CurrentValue == null)
                {
                    if (entry.BaseValue != null)
                    {
                        return (T)entry.BaseValue;
                    }
                }
                else if (entry.CurrentValue != RemovedValue)
                {
                    return (T)entry.CurrentValue;
                }
            }

            return key.DefaultValue;
        }

        public void Set<T>(DataProperty<T> key, T value)
        {
            SetObject(key, value);
        }

        public void SetObject(DataProperty key, object value)
        {
            int index = GetOrCreateKeyIndex(key);

            var entry = values[index];

            if (!object.Equals(entry.CurrentValue, value))
            {
                var oldValue = entry.CurrentValue;
                entry.CurrentValue = value;
                values[index] = entry;

                OnPropertyChanged(key, oldValue, value);
            }
        }

        protected virtual void OnPropertyChanged(DataProperty property, object oldValue, object newValue)
        {
        }

        private int GetKeyIndex(DataProperty key)
        {
            int index = InternalValueBinarySearch(key);
            return index < 0 ? -1 : index;
        }

        private bool GetKeyIndex(DataProperty key, out int index)
        {
            index = GetKeyIndex(key);
            return index >= 0;
        }

        private int InternalValueBinarySearch(DataProperty key)
        {
            int i = 0;
            int end = values.Count - 1;

            while (i <= end)
            {
                int start = i + (end - i >> 1);
                int hashCode = values.Items[start].Key.LocalId;
                int hashCode2 = key.LocalId;

                if (hashCode == hashCode2)
                {
                    return start;
                }
                if (hashCode < hashCode2)
                {
                    i = start + 1;
                }
                else
                {
                    end = start - 1;
                }
            }

            return ~i;
        }

        private int GetOrCreateKeyIndex(DataProperty key)
        {
            int num = InternalValueBinarySearch(key);
            if (num < 0)
            {
                num = ~num;
                values.Insert(num, new ValueEntry(key));
            }
            return num;
        }

        public IEnumerable<KeyValuePair<DataProperty, object>> GetModifiedProperties()
        {
            //if (modifiedValueCount == 0)
            //    yield break;

            for (int i = 0; i < values.Count; i++)
            {
                if (values[i].CurrentValue != null)
                {
                    yield return new KeyValuePair<DataProperty, object>(values[i].Key, values[i].CurrentValue);
                }
            }
        }

        private struct ValueEntry
        {
            public DataProperty Key;

            public object BaseValue;

            public object CurrentValue;

            public ValueEntry(DataProperty key)
            {
                Key = key;
                BaseValue = null;
                CurrentValue = null;
            }
        }
    }
}
