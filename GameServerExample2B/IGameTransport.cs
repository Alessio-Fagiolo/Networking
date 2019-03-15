using System;
using System.Net;

namespace GameServerExample2B
{
    public interface IGameTransport
    {
        void Bind(string address, int port);
        bool Send(byte[] data, EndPoint endPoint);
        byte[] Recv(int bufferSize, ref EndPoint sender);
        EndPoint CreateEndPoint();
    }
}
