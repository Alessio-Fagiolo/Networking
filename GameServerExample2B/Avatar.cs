using System;
namespace GameServerExample2B
{
    public class Avatar : GameObject
    {
        public Avatar(GameServer server) : base(1, server)
        {
        }

        public override void Tick()
        {
            Packet packet = new Packet(3, Id, X, Y, Z);
            packet.OneShot = true;
            server.SendToAllClients(packet);
        }
    }
}
