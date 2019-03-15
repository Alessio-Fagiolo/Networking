using System;

namespace GameServerExample2B
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            GameTransportIPv4 transport = new GameTransportIPv4();
            transport.Bind("192.168.10.1", 9999);

            GameServer server = new GameServer(transport, null);

            Cube cube001 = server.Spawn<Cube>();
            cube001.SetPosition(0, 0, 5);


            Cube cube002 = new Cube(server);
            cube002.SetPosition(0, 3, 0);

            Cube cube003 = new Cube(server);
            cube003.SetPosition(8, 0, 0);

            server.Run();
        }
    }
}
