using System;
using System.Net;
using System.Collections.Generic;

namespace GameServerExample2B
{
    public class GameClient
    {
        private EndPoint endPoint;

        private Queue<Packet> sendQueue;

        private Dictionary<uint, Packet> ackTable;

        public  uint acktablecount { get { return (uint)ackTable.Count; } }

        public uint Malus;

        private GameServer server;

        public GameClient(GameServer server, EndPoint endPoint)
        {
            this.server = server;
            this.endPoint = endPoint;
            sendQueue = new Queue<Packet>();
            ackTable = new Dictionary<uint, Packet>();
            Malus = 0;
        }

        public void Process()
        {
            int packetsInQueue = sendQueue.Count;
            for (int i = 0; i < packetsInQueue; i++)
            {
                Packet packet = sendQueue.Dequeue();
                // check if the packet con be sent
                if (server.Now >= packet.SendAfter)
                {
                    packet.IncreaseAttempts();
                    if (server.Send(packet, endPoint))
                    {
                        // all fine
                        if (packet.NeedAck)
                        {
                            ackTable[packet.Id] = packet;
                        }
                    }
                    // on error, retry sending only if NOT OneShot
                    else if (!packet.OneShot)
                    {
                        if (packet.Attempts < 3)
                        {
                            // retry sending after 1 second
                            packet.SendAfter = server.Now + 1.0f;
                            sendQueue.Enqueue(packet);
                        }
                    }
                }
                else
                {
                    // it is too early, re-enqueue the packet
                    sendQueue.Enqueue(packet);
                }
            }

            // check ack table
            List<uint> deadPackets = new List<uint>();
            foreach (uint id in ackTable.Keys)
            {
                Packet packet = ackTable[id];
                if (packet.IsExpired(server.Now))
                {
                    if (packet.Attempts < 3)
                    {
                        sendQueue.Enqueue(packet);
                    }
                    else
                    {
                        deadPackets.Add(id);
                    }
                }
            }

            foreach (uint id in deadPackets)
            {
                ackTable.Remove(id);
            }
        }

        public void Ack(uint packetId)
        {
            if (ackTable.ContainsKey(packetId))
            {
                ackTable.Remove(packetId);
            }
            // else, increase malus
        }

        public void Enqueue(Packet packet)
        {
            sendQueue.Enqueue(packet);
        }

        public override string ToString()
        {
            return endPoint.ToString();
        }
    }
}
