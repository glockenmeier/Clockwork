using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SiliconStudio.Core;
using SiliconStudio.Core.IO;
using SiliconStudio.Core.Serialization;
using SiliconStudio.Core.Serialization.Assets;
using SiliconStudio.Core.Serialization.Serializers;
using SiliconStudio.Core.Storage;
using SiliconStudio.Core.Collections;
using System.Collections.Specialized;

namespace Clockwork.Data
{
    [DataSerializerGlobal(typeof(DictionaryAllSerializer<TrackingDictionary<ObjectId, object>, ObjectId, object>), typeof(TrackingDictionary<ObjectId, object>))]
    public class DataStore
    {
        private readonly TrackingDictionary<ObjectId, object> items = new TrackingDictionary<ObjectId, object>();
        private readonly Dictionary<object, ObjectId> objectToId = new Dictionary<object, ObjectId>();
        private ServiceRegistry serviceRegistry;

        public static readonly PropertyKey<DataStore> Key = new PropertyKey<DataStore>("Key", typeof(DataStore));

        public DataStore(ServiceRegistry serviceRegistry)
        {
            this.serviceRegistry = serviceRegistry;
            items.CollectionChanged += items_CollectionChanged;
        }

        private void items_CollectionChanged(object sender, TrackingCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    objectToId.Add(e.Item, (ObjectId)e.Key);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    objectToId.Remove(e.Item);
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        public void Add(object item)
        {
            var id = ObjectId.New();
            items.Add(id, item);
        }

        public T Get<T>(ObjectId id)
        {
            return (T)items[id];
        }

        public object GetObject(ObjectId id)
        {
            return items[id];
        }

        public void Remove(object item)
        {
            ObjectId id;
            if (objectToId.TryGetValue(item, out id))
            {
                items.Remove(id);
            }
        }

        public bool TryGetId(object value, out ObjectId id)
        {
            return objectToId.TryGetValue(value, out id);
        }

        public ResolutionContext<ObjectId, object> ResolutionContext { get; private set; }

        public void Load()
        {
            if (!VirtualFileSystem.ApplicationRoaming.FileExists("Savegames/QuickSave01.save"))
                return;

            Unload();

            using (var stream = VirtualFileSystem.ApplicationRoaming.OpenStream("Savegames/QuickSave01.save", VirtualFileMode.Open, VirtualFileAccess.Read))
            {
                var reader = new BinarySerializationReader(stream);
                reader.Context.Tags.Add(Key, this);
                reader.Context.Tags.Add(ServiceRegistry.ServiceRegistryKey, serviceRegistry);

                using (ResolutionContext = items.ToResolutionContext())
                {
                    reader.SerializeExtended(items, ArchiveMode.Deserialize);
                }
            }
        }

        public void Save()
        {
            var obj = new DataObject();
            obj.InheritanceParent = obj;
            Add(obj);

            VirtualFileSystem.ApplicationRoaming.CreateDirectory("Savegames");

            using (var stream = VirtualFileSystem.ApplicationRoaming.OpenStream("Savegames/QuickSave01.save", VirtualFileMode.Create, VirtualFileAccess.Write))
            {
                var writer = new BinarySerializationWriter(stream);
                writer.Context.Tags.Add(Key, this);
                writer.Context.Tags.Add(ServiceRegistry.ServiceRegistryKey, serviceRegistry);

                writer.SerializeExtended(items, ArchiveMode.Serialize);
            }
        }

        public void Unload()
        {
            foreach (var item in items)
            {
            }
        }
    }
}
