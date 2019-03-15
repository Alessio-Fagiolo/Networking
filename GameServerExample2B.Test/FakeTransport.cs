using System;
using System.Net;
using System.Collections.Generic;

namespace GameServerExample2B.Test
{
    public struct FakeData
    {
        public FakeEndPoint endPoint;
        public byte[] data;
        public bool needAck;
    }

    public class FakeQueueEmpty : Exception
    {
    }

    public class FakeTransport : IGameTransport
    {
        private FakeEndPoint boundAddress;

        private Queue<FakeData> recvQueue;
        private Queue<FakeData> sendQueue;

        public FakeTransport()
        {
            recvQueue = new Queue<FakeData>();
            sendQueue = new Queue<FakeData>();
        }

        public void ClientEnqueue(FakeData data)
        {
            recvQueue.Enqueue(data);
        }

        public void ClientEnqueue(Packet packet, string address, int port)
        {
            recvQueue.Enqueue(new FakeData() { data = packet.GetData(), endPoint = new FakeEndPoint(address, port)});
        }

        public FakeData ClientDequeue()
        {
            if (sendQueue.Count <= 0)
                throw new FakeQueueEmpty();
            return sendQueue.Dequeue();
        }

        public void Bind(string address, int port)
        {
            boundAddress = new FakeEndPoint(address, port);
        }

        public EndPoint CreateEndPoint()
        {
            return new FakeEndPoint();
        }

        public byte[] Recv(int bufferSize, ref EndPoint sender)
        {
            FakeData fakeData = recvQueue.Dequeue();
            if (fakeData.data.Length > bufferSize)
                return null;
            sender = fakeData.endPoint;
            return fakeData.data;
        }

        public bool Send(byte[] data, EndPoint endPoint)
        {
            FakeData fakeData = new FakeData();
            fakeData.data = data;
            fakeData.endPoint = endPoint as FakeEndPoint;
            sendQueue.Enqueue(fakeData);
            return true;
        }

        public uint ClientQueueCount
        {
            get
            {
                return (uint)sendQueue.Count;
            }
        }

        
    }
}
