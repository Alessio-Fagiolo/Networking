using System;
namespace GameServerExample2B
{
    public abstract class GameObject
    {
        public float X;
        public float Y;
        public float Z;

        private GameClient owner;
        protected GameServer server;

        public GameClient Owner { get { return owner; } }

        public bool IsOwnedBy(GameClient client)
        {
            return owner == client;
        }

        public void SetOwner(GameClient client)
        {
            owner = client;
        }

        private uint internalObjectType;
        public uint ObjectType
        {
            get
            {
                return internalObjectType;

            }
        }

        private static uint gameObjectCounter;
        private uint internalId;
        public uint Id
        {
            get
            {
                return internalId;
            }
        }

        public GameObject(uint objectType, GameServer server)
        {
            internalObjectType = objectType;
            internalId = ++gameObjectCounter;
            this.server = server;
            Console.WriteLine("spawned GameObject {0} of type {1}", Id, ObjectType);
        }

        public void SetPosition(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public virtual void Tick()
        {

        }
    }
}
