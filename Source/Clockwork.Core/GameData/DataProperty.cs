using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;

namespace Clockwork.Data
{
    public abstract class DataProperty
    {
        public interface IVisitor
        {
            void Visit<T>(DataProperty<T> property);
        }

        public abstract bool IsValueType { get; }

        public Type PropertyType { get; private set; }

        public Type OwnerType { get; private set; }

        public int LocalId { get; private set; }

        public DataProperty(string name, Type propertyType, Type ownerType, int id)
        {
            PropertyType = propertyType;
            OwnerType = ownerType;
            LocalId = id;

            properties.Add(new Key { OwnerType = ownerType, Id = id }, this);
        }

        public abstract void Accept(IVisitor visitor);

        private static readonly Dictionary<Key, DataProperty> properties = new Dictionary<Key, DataProperty>();

        public static DataProperty Find(Type ownerType, int id)
        {
            return properties[new Key { OwnerType = ownerType, Id = id }];
        }

        private struct Key
        {
            public Type OwnerType;
            public int Id;
        }
    }

    public class DataProperty<T> : DataProperty
    {
        private static readonly bool isValueType = typeof(T).GetTypeInfo().IsValueType;

        public T DefaultValue { get; private set; }

        public override bool IsValueType
        {
            get { return isValueType; }
        }

        public DataProperty(string name, Type ownerType, int id)
            : base(name, typeof(T), ownerType, id)
        {
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
