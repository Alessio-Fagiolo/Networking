using System;
using NUnit.Framework;

namespace GameServerExample2B.Test
{
    public class TestGameServer
    {
        private FakeTransport transport;
        private FakeClock clock;
        private GameServer server;

        [SetUp]
        public void SetupTests()
        {
            transport = new FakeTransport();
            clock = new FakeClock();
            server = new GameServer(transport, clock);
        }

        [Test]
        public void TestZeroNow()
        {
            Assert.That(server.Now, Is.EqualTo(0));
        }

        [Test]
        public void TestClientsOnStart()
        {
            Assert.That(server.NumClients, Is.EqualTo(0));
        }

        [Test]
        public void TestGameObjectsOnStart()
        {
            Assert.That(server.NumGameObjects, Is.EqualTo(0));
        }

        [Test]
        public void TestJoinNumOfClients()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            Assert.That(server.NumClients, Is.EqualTo(1));
        }

        [Test]
        public void TestJoinNumOfGameObjects()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            Assert.That(server.NumGameObjects, Is.EqualTo(1));
        }

        [Test]
        public void TestWelcomeAfterJoin()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            FakeData welcome = transport.ClientDequeue();
            Assert.That(welcome.data[0], Is.EqualTo(1));
        }

        [Test]
        public void TestSpawnAvatarAfterJoin()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientDequeue();
            Assert.That(() => transport.ClientDequeue(), Throws.InstanceOf<FakeQueueEmpty>());
        }

        [Test]
        public void TestJoinSameClient()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            Assert.That(server.NumClients, Is.EqualTo(1));
        }

        [Test]
        public void TestJoinSameAddressClient()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientEnqueue(packet, "tester", 1);
            server.SingleStep();
            Assert.That(server.NumClients, Is.EqualTo(2));
        }

        [Test]
        public void TestJoinSameAddressAvatars()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientEnqueue(packet, "tester", 1);
            server.SingleStep();
            Assert.That(server.NumGameObjects, Is.EqualTo(2));
        }

        [Test]
        public void TestJoinTwoClientsSamePort()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientEnqueue(packet, "foobar", 0);
            server.SingleStep();
            Assert.That(server.NumClients, Is.EqualTo(2));
        }

        [Test]
        public void TestJoinTwoClientsWelcome()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientEnqueue(packet, "foobar", 1);
            server.SingleStep();

            Assert.That(transport.ClientQueueCount, Is.EqualTo(5));

            Assert.That(transport.ClientDequeue().endPoint.Address, Is.EqualTo("tester"));
            Assert.That(transport.ClientDequeue().endPoint.Address, Is.EqualTo("tester"));
            Assert.That(transport.ClientDequeue().endPoint.Address, Is.EqualTo("tester"));
            Assert.That(transport.ClientDequeue().endPoint.Address, Is.EqualTo("foobar"));
            Assert.That(transport.ClientDequeue().endPoint.Address, Is.EqualTo("foobar"));
        }

        [Test]
        public void TestEvilUpdate()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            uint IdFromPacket = BitConverter.ToUInt32(transport.ClientDequeue().data, 5);

            transport.ClientEnqueue(packet, "foobar", 1);
            server.SingleStep();

            Packet move = new Packet(3, IdFromPacket, 1.0f, 1.0f, 2.0f);
            transport.ClientEnqueue(move, "foobar", 1);
            server.SingleStep();

            GameObject obj = server.GetGameObjFromID(IdFromPacket);
            Assert.That(obj.X, Is.EqualTo(0));
            Assert.That(obj.Z, Is.EqualTo(0));
            Assert.That(obj.Y, Is.EqualTo(0));

        }

        //check if after a forbidden move command, the bad client has malus increase
        [Test]
        public void TestEvilUpdateMalus()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            FakeData testerJoin = transport.ClientDequeue();
            uint testerId = BitConverter.ToUInt32(testerJoin.data, 5);

            transport.ClientEnqueue(packet, "foobar", 1);
            server.SingleStep();
            transport.ClientDequeue();
            uint foobarId = BitConverter.ToUInt32(transport.ClientDequeue().data, 5);


            Packet move = new Packet(3, foobarId, 1.0f, 1.0f, 2.0f);
            transport.ClientEnqueue(move, "tester", 0);
            server.SingleStep();

            GameClient client = server.GetClientFromEndPoint(testerJoin.endPoint);
            Assert.That(client.Malus, Is.EqualTo(1));

        }


        //checking the correct position of a gameobjet after send move packet;
        [Test]
        public void TestGoodUpdate()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            uint IdFromPacket = BitConverter.ToUInt32(transport.ClientDequeue().data, 5);

            Packet move = new Packet(3, IdFromPacket, 1.0f, 1.0f, 2.0f);
            transport.ClientEnqueue(move, "tester", 0);
            server.SingleStep();

            GameObject obj = server.GetGameObjFromID(IdFromPacket);
            Assert.That(obj.X, Is.EqualTo(1));
            Assert.That(obj.Z, Is.EqualTo(2));
            Assert.That(obj.Y, Is.EqualTo(1));

        }

        //if the command of a packet isn't in the commandsTable, is generated an exception.
        [Test]
        public void checkUnknownCommand()
        {
            Packet packet = new Packet(5);
            transport.ClientEnqueue(packet, "tester", 0);
            Assert.That(() => server.SingleStep(), Throws.Exception);
        }

        //test if after an allowed command client's malus is equals to 0
        [Test]
        public void CheckMalusAfterAvaiableCommand()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            GameClient client = server.GetClientFromEndPoint(transport.ClientDequeue().endPoint);
            Assert.That(client.Malus, Is.EqualTo(0));

        }

        //test if after an unknown command client's malus is increased
        [Test]
        public void CheckMalusAfterUnknownCommand()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            GameClient client = server.GetClientFromEndPoint(transport.ClientDequeue().endPoint);
            Packet evilPacket = new Packet(5);
            transport.ClientEnqueue(evilPacket, "tester", 0);
            server.SingleStep();
            Assert.That(client.Malus, Is.EqualTo(1));

        }

        // test if malus is increased after a client join twice
        [Test]
        public void checkMalusAfterJoinSameClient()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            GameClient client = server.GetClientFromEndPoint(transport.ClientDequeue().endPoint);
            Assert.That(client.Malus, Is.GreaterThan(0));
            
        }


        //checking there is an Ack Packet in the client's queue after it receves the welcome packet
        [Test]
        public void checkWelcomeAck()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();

            GameClient client = server.GetClientFromEndPoint(transport.ClientDequeue().endPoint);
            Assert.That(client.acktablecount, Is.EqualTo(1));

        }


        //checking if the server.now is equal to a value we decide to set
        [Test]
        public void SetClock()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            clock.setClock = 3;
            server.SingleStep();
            Assert.That(server.getclock().GetNow, Is.EqualTo(3.0f));

        }

        //checking if the server.now is equal to a value increased after a step
        [Test]
        public void TimeAfterStart()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            clock.IncreaseTimeStamp(1);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            Assert.That(server.Now, Is.EqualTo(1));
        }

        // checking the position of a gameobject after several moves and steps
        [Test]
        public void CheckVelocityInTime()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            uint IdFromPacket = BitConverter.ToUInt32(transport.ClientDequeue().data, 5);

            Packet move = new Packet(3, IdFromPacket, 1.0f, 1.0f, 2.0f);
            transport.ClientEnqueue(move, "tester", 0);
            clock.IncreaseTimeStamp(1);
            server.SingleStep();

            Packet move2 = new Packet(3, IdFromPacket, 2.0f, 2.0f, 2.0f);
            transport.ClientEnqueue(move2, "tester", 0);
            clock.IncreaseTimeStamp(1);
            server.SingleStep();

            GameObject obj = server.GetGameObjFromID(IdFromPacket);
            Assert.That(obj.X, Is.EqualTo(3));
            Assert.That(obj.Z, Is.EqualTo(4));
            Assert.That(obj.Y, Is.EqualTo(3));
            Assert.That(server.Now, Is.EqualTo(2));
        }

   


        //david
        [Test]
        public void checkPackeNeedAck()
        {
            Packet packet = new Packet(0);
            packet.NeedAck = true;
            transport.ClientEnqueue(packet, "test", 0);
            server.SingleStep();
            Assert.That(packet.NeedAck, Is.True);
        }

        //check if there is a spawn packet after Join
        [Test]
        public void TestSpawnAfterJoin()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientEnqueue(packet, "foobar", 1 );
            server.SingleStep();
            transport.ClientDequeue();
            transport.ClientDequeue();
            FakeData spawn = transport.ClientDequeue();
            Assert.That(spawn.data[0], Is.EqualTo(2));
        }

        //check if there is a Null endpoint
        [Test]
        public void checkNullEndpoint()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "test", 0);
            server.SingleStep();
            Assert.That(transport.ClientDequeue().endPoint, Is.Not.EqualTo(null));
        }

        //check if there is a Null address
        [Test]
        public void checkNullAddress()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "test", 0);
            server.SingleStep();
            Assert.That(transport.ClientDequeue().endPoint.Address, Is.Not.EqualTo(null));
        }

        //check if there is a Null port
        [Test]
        public void checkPortNull()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "test", 0);
            server.SingleStep();

            Assert.That(transport.ClientDequeue().endPoint.Port, Is.Not.EqualTo(null));
        }

        // check if the owner of a GO is the client that joined
        [Test]
        public void checkGameObjectOwner()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            FakeData pacchetto = transport.ClientDequeue();
            uint IdFromPacket = BitConverter.ToUInt32(pacchetto.data, 5);
            GameObject obj = server.GetGameObjFromID(IdFromPacket);
            GameClient client = server.GetClientFromEndPoint(pacchetto.endPoint);

            Assert.That(obj.Owner, Is.EqualTo(client));

        }

    }
}
