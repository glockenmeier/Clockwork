using SiliconStudio.Core;
using SiliconStudio.Core.Serialization;
using SiliconStudio.Core.Serialization.Contents;
using SiliconStudio.Core.Serialization.Serializers;
using System;

namespace Clockwork
{
    [DataContract]
    [DataSerializer(typeof(HeightMapSerializer))]
    [ContentSerializer(typeof(DataContentSerializer<HeightMap>))]
    public sealed class HeightMap
    {
        public float Resolution = 100.0f / 64;

        public int Height
        {
            get { return Data.GetLength(0); }
        }

        public int Width
        {
            get { return Data.GetLength(1); }
        }

        public float[,] Data;

        public float this[int y, int x]
        {
            get { return Data[y, x]; }
            set { Data[y, x] = value; }
        }

        public HeightMap()
        {
        }

        public HeightMap(int width, int height)
        {
            Data = new float[height, width];
        }

        public HeightMap(float[,] data)
        {
            Data = data;
        }
    }

    public class HeightMapSerializer : ClassDataSerializer<HeightMap>
    {
        public override void Serialize(ref HeightMap obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                int height = stream.ReadInt32();
                int width = stream.ReadInt32();

                float offset = stream.ReadSingle();
                obj.Resolution = stream.ReadSingle();

                if (obj.Data == null || obj.Width != width || obj.Height != height)
                {
                    obj.Data = new float[height, width];
                }

                for (int y = 0; y < height; y++)
                {
                    float value;

                    int encodedValue = stream.ReadPackedInt();
                    offset += obj.Resolution * MathUtilities.ZigZagDecode(encodedValue);
                    obj.Data[y, 0] = value = offset;

                    for (int x = 1; x < width; x++)
                    {
                        encodedValue = stream.ReadPackedInt();
                        value += obj.Resolution * MathUtilities.ZigZagDecode(encodedValue);
                        obj.Data[y, x] = value;
                    }
                }
            }
            else
            {
                stream.Write(obj.Height);
                stream.Write(obj.Width);

                stream.Write(obj.Data[0, 0]);
                stream.Write(obj.Resolution);

                float scale = 1.0f / obj.Resolution;
                float offset = obj.Data[0, 0];

                for (int y = 0; y < obj.Height; y++)
                {
                    float data = obj.Data[y, 0];
                    float step = data - offset;
                    int encodedValue = MathUtilities.ZigZagEncode((int)Math.Round(step * scale));
                    stream.WritePackedInt(encodedValue);
                    float value = offset = data;

                    for (int x = 1; x < obj.Width; x++)
                    {
                        data = obj.Data[y, x];
                        step = data - value;
                        value = data;
                        encodedValue = MathUtilities.ZigZagEncode((int)Math.Round(step * scale));
                        stream.WritePackedInt(encodedValue);
                    }
                }
            }
        }
    }
}
