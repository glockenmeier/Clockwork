using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SiliconStudio.Core;
using SiliconStudio.Core.Serialization;
using SiliconStudio.Core.Serialization.Assets;
using SiliconStudio.Core.Storage;

namespace Clockwork.Data
{
    public class AssetLoadVisitor : DataProperty.IVisitor
    {
        private IAssetManager assetManager;
        private string url;
        private DataObject instance;

        void DataProperty.IVisitor.Visit<T>(DataProperty<T> property)
        {
            T value = default(T);//assetManager.Load<T>(url);
            instance.Set(property, value);
        }

        public void Load(DataObject instance, DataProperty property, string url, IAssetManager assetManager)
        {
            this.assetManager = assetManager;
            this.url = url;
            this.instance = instance;

            property.Accept(this);

            this.instance = null;
            this.url = null;
            this.assetManager = null;
        }
    }

    public class DataObjectSerializer<T> : ClassDataSerializer<T>, IDataSerializerInitializer
        where T : DataObject, new()
    {
        private DataSerializer valueSerializer;

        public void Initialize(SerializerSelector serializerSelector)
        {
            //keySerializer = MemberSerializer<TKey>.Create(serializerSelector, false);
            valueSerializer = MemberSerializer<object>.Create(serializerSelector);
        }

        public override void Serialize(ref T obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                int count = stream.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    var id = stream.ReadInt32();
                    var propertyKey = DataProperty.Find(typeof(T), id);
                    object value = null;

                    byte flags = stream.ReadByte();

                    switch (flags)
                    {
                        case 0:
                            valueSerializer.Serialize(ref value, mode, stream);
                            obj.SetObject(propertyKey, value);
                            break;

                        case 1:
                            ReadReference(stream, obj, propertyKey);
                            break;

                            /*
                        case 2:
                            var services = stream.Context.Tags.Get(ServiceRegistry.ServiceRegistryKey);
                            var assets = services.GetSafeServiceAs<AssetManager>();
                            var url = stream.ReadString();
                            break;
                            */
                        default:
                            throw new InvalidCastException();
                    }
                }
            }
            else
            {
                var modifiedProperties = obj.GetModifiedProperties().ToList();

                stream.Write(modifiedProperties.Count);
                foreach (var item in obj.GetModifiedProperties())
                {
                    stream.Write(item.Key.LocalId);
                    object value = item.Value;

                    ObjectId reference;
                    if (stream.Context.Tags.Get(DataStore.Key).TryGetId(value, out reference))
                    {
                        stream.Write((byte)1);
                        stream.Write(reference);
                    }
                    else
                    {
                        /*
                        string url;
                        var services = stream.Context.Tags.Get(ServiceRegistry.ServiceRegistryKey);
                        var assets = services.GetSafeServiceAs<AssetManager>();

                        if (!item.Key.IsValueType && assets.TryGetAssetUrl(value, out url))
                        {
                            stream.Write((byte)2);
                            stream.Write(url);
                        }
                        else*/
                        {
                            stream.Write((byte)0);
                            valueSerializer.Serialize(ref value, mode, stream);
                        }
                    }
                }
            }
        }

        private async void ReadReference(SerializationStream stream, DataObject instance, DataProperty property)
        {
            ObjectId reference = stream.Read<ObjectId>();

            object value = await stream.Context.Tags.Get(DataStore.Key).ResolutionContext.Resolve<object>(reference);

            instance.SetObject(property, value);
        }
    }
}
