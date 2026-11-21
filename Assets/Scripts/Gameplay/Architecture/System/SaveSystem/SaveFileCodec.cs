using System;
using MessagePack;
using MessagePack.Resolvers;
using MessagePack.Unity;

namespace Gameplay
{
    internal static class SaveFileCodec
    {
        public const int HeaderSize = 24;

        private const ushort FormatVersion = 1;
        private static readonly MessagePackSerializerOptions SerializerOptions = MessagePackSerializerOptions.Standard.WithResolver(CompositeResolver.Create(GeneratedMessagePackResolver.Instance, UnityResolver.Instance, StandardResolver.Instance));

        public static byte[] Encode(GameSaveData saveData)
        {
            if (saveData == null)
            {
                throw new ArgumentNullException(nameof(saveData));
            }

            var payload = MessagePackSerializer.Serialize(saveData, SerializerOptions);
            if (payload.Length == 0)
            {
                throw new InvalidOperationException("Save payload cannot be empty.");
            }

            var bytes = new byte[HeaderSize + payload.Length];
            bytes[0] = (byte)'Y';
            bytes[1] = (byte)'H';
            bytes[2] = (byte)'S';
            bytes[3] = (byte)'V';
            WriteUInt16(bytes, 4, FormatVersion);
            WriteUInt16(bytes, 6, HeaderSize);
            WriteInt32(bytes, 8, saveData.SceneId);
            WriteInt64(bytes, 12, saveData.SaveTimeUtcTicks);
            WriteInt32(bytes, 20, payload.Length);
            Buffer.BlockCopy(payload, 0, bytes, HeaderSize, payload.Length);
            return bytes;
        }

        public static GameSaveData Decode(byte[] bytes)
        {
            var payload = GetPayload(bytes, out var header);
            var memory = new ReadOnlyMemory<byte>(payload.Array, payload.Offset, payload.Count);
            var saveData = MessagePackSerializer.Deserialize<GameSaveData>(memory, SerializerOptions);
            if (saveData == null)
            {
                throw new InvalidOperationException("Save data is empty.");
            }
            if (saveData.SceneId != header.SceneId || saveData.SaveTimeUtcTicks != header.SaveTimeUtcTicks)
            {
                throw new InvalidOperationException("Save file header does not match its payload.");
            }
            return saveData;
        }

        public static bool TryReadSummary(byte[] bytes, out int sceneId, out long saveTimeUtcTicks)
        {
            if (!TryReadHeader(bytes, out var header, out _))
            {
                sceneId = default;
                saveTimeUtcTicks = default;
                return false;
            }

            sceneId = header.SceneId;
            saveTimeUtcTicks = header.SaveTimeUtcTicks;
            return true;
        }

        private static bool TryReadHeader(byte[] bytes, out Header header, out string error)
        {
            header = default;
            if (bytes == null || bytes.Length < HeaderSize)
            {
                error = "Save file header is incomplete.";
                return false;
            }
            if (bytes[0] != (byte)'Y' || bytes[1] != (byte)'H' || bytes[2] != (byte)'S' || bytes[3] != (byte)'V')
            {
                error = "Save file magic is invalid.";
                return false;
            }
            if (ReadUInt16(bytes, 4) != FormatVersion)
            {
                error = "Save file version is unsupported.";
                return false;
            }
            if (ReadUInt16(bytes, 6) != HeaderSize)
            {
                error = "Save file header size is invalid.";
                return false;
            }

            var sceneId = ReadInt32(bytes, 8);
            var saveTimeUtcTicks = ReadInt64(bytes, 12);
            var payloadLength = ReadInt32(bytes, 20);
            if (sceneId <= 0 || saveTimeUtcTicks <= 0 || saveTimeUtcTicks > DateTime.MaxValue.Ticks || payloadLength <= 0)
            {
                error = "Save file header data is invalid.";
                return false;
            }

            header = new Header(sceneId, saveTimeUtcTicks, payloadLength);
            error = null;
            return true;
        }

        private static ArraySegment<byte> GetPayload(byte[] bytes, out Header header)
        {
            if (!TryReadHeader(bytes, out header, out var error))
            {
                throw new InvalidOperationException(error);
            }
            if (header.PayloadLength != bytes.Length - HeaderSize)
            {
                throw new InvalidOperationException("Save file payload length does not match the header.");
            }
            return new ArraySegment<byte>(bytes, HeaderSize, header.PayloadLength);
        }

        private static void WriteUInt16(byte[] bytes, int offset, int value)
        {
            bytes[offset] = (byte)value;
            bytes[offset + 1] = (byte)(value >> 8);
        }

        private static ushort ReadUInt16(byte[] bytes, int offset)
        {
            return (ushort)(bytes[offset] | bytes[offset + 1] << 8);
        }

        private static void WriteInt32(byte[] bytes, int offset, int value)
        {
            bytes[offset] = (byte)value;
            bytes[offset + 1] = (byte)(value >> 8);
            bytes[offset + 2] = (byte)(value >> 16);
            bytes[offset + 3] = (byte)(value >> 24);
        }

        private static int ReadInt32(byte[] bytes, int offset)
        {
            return bytes[offset] | bytes[offset + 1] << 8 | bytes[offset + 2] << 16 | bytes[offset + 3] << 24;
        }

        private static void WriteInt64(byte[] bytes, int offset, long value)
        {
            for (var index = 0; index < sizeof(long); index++)
            {
                bytes[offset + index] = (byte)(value >> (index * 8));
            }
        }

        private static long ReadInt64(byte[] bytes, int offset)
        {
            long value = 0;
            for (var index = 0; index < sizeof(long); index++)
            {
                value |= (long)bytes[offset + index] << (index * 8);
            }
            return value;
        }

        private readonly struct Header
        {
            public int SceneId { get; }
            public long SaveTimeUtcTicks { get; }
            public int PayloadLength { get; }

            public Header(int sceneId, long saveTimeUtcTicks, int payloadLength)
            {
                SceneId = sceneId;
                SaveTimeUtcTicks = saveTimeUtcTicks;
                PayloadLength = payloadLength;
            }
        }
    }
}
