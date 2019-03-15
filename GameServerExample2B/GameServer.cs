using System;
using System.Net;
using System.Diagnostics;
using System.Collections.Generic;

namespace GameServerExample2B
{
    public class GameServer
    {

        private delegate void GameCommand(byte[] data, EndPoint sender);

        private Dictionary<byte, GameCommand> commandsTable;

        private Dictionary<EndPoint, GameClient> clientsTable;
        private Dictionary<uint, GameObject> gameObjectsTable;

        private IGameTransport transport;
        private IMonotonicClock clock;

        public uint NumClients
        {
            get
            {
                return (uint)clientsTable.Count;
            }
        }

        public uint NumGameObjects
        {
            get
            {
                return (uint)gameObjectsTable.Count;
            }
        }

        private void Join(byte[] data, EndPoint sender)
        {
            // check if the client has already joined
            if (clientsTable.ContainsKey(sender))
            {
                GameClient badClient = clientsTable[sender];
                badClient.Malus++;
                return;
            }

            GameClient newClient = new GameClient(this, sender);
            clientsTable[sender] = newClient;
            Avatar avatar = Spawn<Avatar>();
            avatar.SetOwner(newClient);
            Packet welcome = new Packet(1, avatar.ObjectType, avatar.Id, avatar.X, avatar.Y, avatar.Z);
            welcome.NeedAck = true;
            newClient.Enqueue(welcome);

            // spawn all server's objects in the new client
            foreach (GameObject gameObject in gameObjectsTable.Values)
            {
                // ignore myself
                if (gameObject == avatar)
                    continue;
                Packet spawn = new Packet(2, gameObject.ObjectType, gameObject.Id, gameObject.X, gameObject.Y, gameObject.Z);
                spawn.NeedAck = true;
                newClient.Enqueue(spawn);
            }


            // informs the other clients about the new one
            Packet newClientSpawned = new Packet(2, avatar.ObjectType, avatar.Id, avatar.X, avatar.Y, avatar.Z);
            newClientSpawned.NeedAck = true;
            SendToAllClientsExceptOne(newClientSpawned, newClient);

            Console.WriteLine("client {0} joined with avatar {1}", newClient, avatar.Id);
        }

        private void Ack(byte[] data, EndPoint sender)
        {
            if (!clientsTable.ContainsKey(sender))
            {
                return;
            }

            GameClient client = clientsTable[sender];
            uint packetId = BitConverter.ToUInt32(data, 1);
            client.Ack(packetId);
        }

        private void Update(byte[] data, EndPoint sender)
        {
            if (!clientsTable.ContainsKey(sender))
            {
                return;
            }
         
            GameClient client = clientsTable[sender];
            uint netId = BitConverter.ToUInt32(data, 1);
            if (gameObjectsTable.ContainsKey(netId))
            {
                GameObject gameObject = gameObjectsTable[netId];
                if (gameObject.IsOwnedBy(client))
                {
                    float x = BitConverter.ToSingle(data, 5);
                    float y = BitConverter.ToSingle(data, 9);
                    float z = BitConverter.ToSingle(data, 13);
                    gameObject.SetPosition(x + gameObject.X, y + gameObject.Y, z + gameObject.Z);
                }
                else
                {
                    client.Malus++;
                }
              

            }
            
        }

        public GameServer(IGameTransport gameTransport, IMonotonicClock clock)
        {
            transport = gameTransport;
            this.clock = clock;
            clientsTable = new Dictionary<EndPoint, GameClient>();
            gameObjectsTable = new Dictionary<uint, GameObject>();
            commandsTable = new Dictionary<byte, GameCommand>();
            commandsTable[0] = Join;
            commandsTable[3] = Update;
            commandsTable[255] = Ack;
        }


        public void Run()
        {

            Console.WriteLine("server started");
            while (true)
            {
                SingleStep();
            }
        }

        private float currentNow;
        public float Now
        {
            get
            {
                return currentNow;
            }
        }

        public void SingleStep()
        {
            currentNow = clock.GetNow();
            EndPoint sender = transport.CreateEndPoint();
            byte[] data = transport.Recv(256, ref sender);
            if (data != null)
            {
                byte gameCommand = data[0];
                if (commandsTable.ContainsKey(gameCommand))
                {
                    commandsTable[gameCommand](data, sender);
                }
                else
                {
                    clientsTable[sender].Malus++;
                }
            }

            foreach (GameClient client in clientsTable.Values)
            {
                client.Process();
            }

            foreach (GameObject gameObject in gameObjectsTable.Values)
            {
                gameObject.Tick();
            }


        }


        public bool Send(Packet packet, EndPoint endPoint)
        {
            return transport.Send(packet.GetData(), endPoint);
        }

        public void SendToAllClients(Packet packet)
        {
            foreach (GameClient client in clientsTable.Values)
            {
                client.Enqueue(packet);
            }
        }

        public void SendToAllClientsExceptOne(Packet packet, GameClient except)
        {
            foreach (GameClient client in clientsTable.Values)
            {
                if (client != except)
                    client.Enqueue(packet);
            }
        }

        public void RegisterGameObject(GameObject gameObject)
        {
            if (gameObjectsTable.ContainsKey(gameObject.Id))
                throw new Exception("GameObject already registered");
            gameObjectsTable[gameObject.Id] = gameObject;
        }

        public T Spawn<T>() where T : GameObject
        {
            object[] ctorParams = { this };
            T newGameObject = Activator.CreateInstance(typeof(T), ctorParams) as T;
            RegisterGameObject(newGameObject);
            return newGameObject;
        }

        public GameClient GetClientFromEndPoint(EndPoint endpoint)
        {
            if (clientsTable.ContainsKey(endpoint))
            {
                return clientsTable[endpoint];
            }
            return null;
        }

        public GameObject GetGameObjFromID(uint id)
        {
            if (gameObjectsTable.ContainsKey(id))
            {
                return gameObjectsTable[id];
            }

            return null;
        }

        public IMonotonicClock getclock()
        {
            return clock;

        }

    }
}
