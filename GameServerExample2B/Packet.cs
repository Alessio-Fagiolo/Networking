using System;
using System.IO;

namespace GameServerExample2B
{
    public class Packet
    {
        private MemoryStream stream;
        private BinaryWriter writer;

        private static uint packetCounter;

        public float SendAfter;
        public bool OneShot;

        public bool NeedAck;

        private uint id;
        public uint Id
        {
            get
            {
                return id;
            }
        }

        private float expires;
        public bool IsExpired(float now)
        {
            return expires < now;
        }

        public void SetExpire(float death)
        {
            expires = death;
        }

        private uint attempts;
        public uint Attempts
        {
            get
            {
                return attempts;
            }
        }

        public void IncreaseAttempts()
        {
            attempts++;
        }

        public Packet()
        {
            stream = new MemoryStream();
            writer = new BinaryWriter(stream);
            id = ++packetCounter;
            attempts = 0;
            OneShot = false;
            SendAfter = 0;
        }

        public Packet(byte command, params object[] elements) : this()
        {
            // first element is always the command
            writer.Write(command);
            foreach (object element in elements)
            {
                if (element is int)
                {
                    writer.Write((int)element);
                }
                else if (element is float)
                {
                    writer.Write((float)element);
                }
                else if (element is byte)
                {
                    writer.Write((byte)element);
                }
                else if (element is char)
                {
                    writer.Write((char)element);
                }
                else if (element is uint)
                {
                    writer.Write((uint)element);
                }
                else
                {
                    throw new Exception("unknown type");
                }
            }
        }

        public byte[] GetData()
        {
            return stream.ToArray();
        }
    }
}
