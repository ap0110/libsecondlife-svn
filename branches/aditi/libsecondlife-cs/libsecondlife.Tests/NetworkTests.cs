using System;
using System.Collections.Generic;
using System.Net;
using libsecondlife;
using libsecondlife.Packets;
using libsecondlife.Utilities;
using NUnit.Framework;

namespace libsecondlife.Tests
{
    [TestFixture]
    public class NetworkTests : Assert
    {
        SecondLife Client;

        ulong CurrentRegionHandle = 0;
        ulong AhernRegionHandle = 1096213093149184;
        ulong MorrisRegionHandle = 1096213093149183;
        bool DetectedObject = false;
        bool DoneTeleporting = false;
        MainAvatar.TeleportStatus tpStatus = MainAvatar.TeleportStatus.None;
        string tpMessage = "";

        LLUUID LookupKey1 = new LLUUID("25472683cb324516904a6cd0ecabf128");
        string LookupName1 = "Bot Ringo";

        public NetworkTests()
        {
            Client = new SecondLife();

            string startLoc = NetworkManager.StartLocation("hooper", 128, 128, 32);

            // Register callbacks
            Client.Network.RegisterCallback(PacketType.ObjectUpdate, new NetworkManager.PacketCallback(ObjectUpdateHandler));
            Client.Self.OnTeleport += new MainAvatar.TeleportCallback(OnTeleportHandler);

            Client.Network.Login("Testing", "Anvil", "testinganvil", "Unit Test Framework", startLoc,
                "contact@libsecondlife.org", false);
        }

        ~NetworkTests()
        {
            Client.Network.Logout();
        }

        [SetUp]
        public void Init()
        {
            Assert.IsTrue(Client.Network.Connected, "Client is not connected to the grid: " + Client.Network.LoginError);

            int start = Environment.TickCount;
            while (Client.Network.CurrentSim.Region.Name == "")
            {
                if (Environment.TickCount - start > 5000)
                {
                    Assert.Fail("Timeout waiting for a RegionHandshake packet");
                }
            }

            //Assert.AreEqual("ahern", Client.Network.CurrentSim.Region.Name.ToLower(), "Logged in to sim " + 
            //    Client.Network.CurrentSim.Region.Name + " instead of Ahern");
        }

        [Test]
        public void DetectObjects()
        {
            int start = Environment.TickCount;
            while (!DetectedObject)
            {
                if (Environment.TickCount - start > 10000)
                {
                    Assert.Fail("Timeout waiting for an ObjectUpdate packet");
                    return;
                }
            }
        }

        [Test]
        public void U64Receive()
        {
            int start = Environment.TickCount;
            while (CurrentRegionHandle == 0)
            {
                if (Environment.TickCount - start > 10000)
                {
                    Assert.Fail("Timeout waiting for an ObjectUpdate packet");
                    return;
                }
            }

            Assert.IsTrue(CurrentRegionHandle == AhernRegionHandle, "Current region is " +
                CurrentRegionHandle + " when we were expecting " + AhernRegionHandle + ", possible endian issue");
        }

        [Test]
        public void NameLookup()
        {
            AvatarTracker tracker = new AvatarTracker(Client);

            string name = tracker.GetAvatarName(LookupKey1);

            Assert.IsTrue(name == LookupName1, "AvatarTracker.GetAvatarName() returned " + name +
                " instead of " + LookupName1);
        }

        [Test]
        public void Teleport()
        {
            DoneTeleporting = false;
            tpStatus = MainAvatar.TeleportStatus.None;

            Client.Self.Teleport(MorrisRegionHandle, new LLVector3(128, 128, 32));

            int start = Environment.TickCount;

            while (!DoneTeleporting)
            {
                System.Threading.Thread.Sleep(100);

                if (Environment.TickCount - start > 10000)
                {
                    Assert.Fail("Timeout waiting for the first teleport to finish");
                    return;
                }
            }

            Assert.IsTrue(tpStatus == MainAvatar.TeleportStatus.Finished, 
                "Teleport status is " + tpStatus.ToString() + ", message=" + tpMessage);

            // Wait for the region information to come in
            start = Environment.TickCount;
            while (Client.Network.CurrentSim.Region.Name == "")
            {
                if (Environment.TickCount - start > 5000)
                {
                    Assert.Fail("Timeout waiting for a RegionHandshake packet");
                }
            }

            // Assert that we really did make it to our scheduled destination
            Assert.AreEqual("morris", Client.Network.CurrentSim.Region.Name.ToLower(),
                "Expected to teleport to Morris, ended up in " + Client.Network.CurrentSim.Region.Name +
                ". Possibly region full or offline?");

            ///////////////////////////////////////////////////////////////////

            // TODO: Add a local region teleport

            ///////////////////////////////////////////////////////////////////

            DoneTeleporting = false;
            tpStatus = MainAvatar.TeleportStatus.None;

            Client.Self.Teleport(AhernRegionHandle, new LLVector3(128, 128, 32));

            start = Environment.TickCount;

            while (!DoneTeleporting)
            {
                System.Threading.Thread.Sleep(100);

                if (Environment.TickCount - start > 10000)
                {
                    Assert.Fail("Timeout waiting for the second teleport to finish");
                    return;
                }
            }

            Assert.IsTrue(tpStatus == MainAvatar.TeleportStatus.Finished, "Teleport status is " + 
                tpStatus.ToString() + ", message=" + tpMessage);

            // Wait for the region information to come in
            start = Environment.TickCount;
            while (Client.Network.CurrentSim.Region.Name == "")
            {
                if (Environment.TickCount - start > 5000)
                {
                    Assert.Fail("Timeout waiting for a RegionHandshake packet");
                }
            }

            // Assert that we really did make it to our scheduled destination
            Assert.AreEqual("ahern", Client.Network.CurrentSim.Region.Name.ToLower(),
                "Expected to teleport to Ahern, ended up in " + Client.Network.CurrentSim.Region.Name +
                ". Possibly region full or offline?");
        }

        private void ObjectUpdateHandler(Packet packet, Simulator sim)
        {
            ObjectUpdatePacket update = (ObjectUpdatePacket)packet;

            DetectedObject = true;
            CurrentRegionHandle = update.RegionData.RegionHandle;
        }

        private void OnTeleportHandler(Simulator currentSim, string message, MainAvatar.TeleportStatus status)
        {
            switch (status)
            {
                case MainAvatar.TeleportStatus.None:
                    break;
                case MainAvatar.TeleportStatus.Start:
                    break;
                case MainAvatar.TeleportStatus.Progress:
                    break;
                case MainAvatar.TeleportStatus.Failed:
                    DoneTeleporting = true;
                    break;
                case MainAvatar.TeleportStatus.Finished:
                    DoneTeleporting = true;
                    break;
            }

            tpMessage = message;
            tpStatus = status;
        }

        [TearDown]
        public void Shutdown()
        {
            //Client.Network.Logout();
            //Client = null;
        }
    }
}
