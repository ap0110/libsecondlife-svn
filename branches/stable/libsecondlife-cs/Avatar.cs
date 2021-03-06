/*
 * Copyright (c) 2006, Second Life Reverse Engineering Team
 * All rights reserved.
 *
 * - Redistribution and use in source and binary forms, with or without
 *   modification, are permitted provided that the following conditions are met:
 *
 * - Redistributions of source code must retain the above copyright notice, this
 *   list of conditions and the following disclaimer.
 * - Neither the name of the Second Life Reverse Engineering Team nor the names
 *   of its contributors may be used to endorse or promote products derived from
 *   this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Timers;
using System.Net;
using System.Collections.Generic;
using libsecondlife.Packets;

namespace libsecondlife
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Message"></param>
    /// <param name="Audible"></param>
    /// <param name="Type"></param>
    /// <param name="Sourcetype"></param>
    /// <param name="FromName"></param>
    /// <param name="ID"></param>
    public delegate void ChatCallback(string message, byte audible, byte type, byte sourcetype,
        string fromName, LLUUID id);

    /// <summary>
    /// Triggered when the L$ account balance for this avatar changes
    /// </summary>
    /// <param name="balance">The new account balance</param>
    public delegate void BalanceCallback(int balance);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="fromAgentID"></param>
    /// <param name="fromAgentName"></param>
    /// <param name="toAgentID"></param>
    /// <param name="parentEstateID"></param>
    /// <param name="regionID"></param>
    /// <param name="position"></param>
    /// <param name="dialog"></param>
    /// <param name="groupIM"></param>
    /// <param name="imSessionID"></param>
    /// <param name="timestamp"></param>
    /// <param name="message"></param>
    /// <param name="offline"></param>
    /// <param name="binaryBucket"></param>
    public delegate void InstantMessageCallback(LLUUID fromAgentID, string fromAgentName, 
        LLUUID toAgentID, uint parentEstateID, LLUUID regionID, LLVector3 position, 
        byte dialog, bool groupIM, LLUUID imSessionID, DateTime timestamp, string message, 
        byte offline, byte[] binaryBucket);

    /// <summary>
    /// Triggered for any status updates of a teleport (progress, failed, succeeded)
    /// </summary>
    /// <param name="message">A message about the current teleport status</param>
    public delegate void TeleportCallback(string message, TeleportStatus status);

    /// <summary>
    /// Name Conversion for Teleport Status flag/bit
    /// </summary>
    public enum TeleportStatus
    {
        None,
        Start,
        Progress,
        Failed,
        Finished
    }

    /// <summary>
    /// Basic class to hold other Avatar's data.
    /// </summary>
    public class Avatar
    {
        /// <summary>The Avatar's UUID, asset server</summary>
        public LLUUID ID;
        /// <summary>Avatar ID in Region (sim) it is in</summary>
        public uint LocalID;
        /// <summary>Full Name of Avatar</summary>
        public string Name;
        /// <summary>Active Group of Avatar</summary>
        public string GroupName;
        /// <summary>Online Status of Avatar</summary>
        public bool Online;
        /// <summary>Location of Avatar (x,y,z probably)</summary>
        public LLVector3 Position;
        /// <summary>Rotational Position of Avatar</summary>
        public LLQuaternion Rotation;
        /// <summary>Region (aka sim) the Avatar is in</summary>
        public Region CurrentRegion;

        public string BornOn;
        
        public LLUUID ProfileImage;

        public LLUUID PartnerID;

        public string AboutText;

        public uint WantToMask;

        public string WantToText;
        
        public uint SkillsMask;

        public string SkillsText;

        public string FirstLifeText;

        public LLUUID FirstLifeImage;

        public bool Identified;

        public bool Transacted;

        public bool AllowPublish;

        public bool MaturePublish;

        public string CharterMember;

        public float Behavior;

        public float Appearance;

        public float Building;

        public string LanguagesText;

        public TextureEntry Textures;

        public string ProfileURL;
    }

    /// <summary>
    /// Class to hold Client Avatar's data
    /// </summary>
    public class MainAvatar
    {
        /// <summary>Callback for incoming chat packets</summary>
        public event ChatCallback OnChat;
        /// <summary>Callback for incoming IMs</summary>
        public event InstantMessageCallback OnInstantMessage;
        /// <summary>Callback for Teleport request update</summary>
        public event TeleportCallback OnTeleport;
        /// <summary>Callback for incoming change in L$ balance</summary>
        public event BalanceCallback OnBalanceUpdated;

        /// <summary>Your (client) Avatar UUID, asset server</summary>
        public LLUUID ID;
        /// <summary>Your (client) Avatar ID, local to Region/sim</summary>
        public uint LocalID;
        /// <summary>Avatar First Name (i.e. Philip)</summary>
        public string FirstName;
        /// <summary>Avatar Last Name (i.e. Linden)</summary>
        public string LastName;
        /// <summary></summary>
        public string TeleportMessage;
        /// <summary>Current position of avatar</summary>
        public LLVector3 Position;
        /// <summary>Current rotation of avatar</summary>
        public LLQuaternion Rotation;
        /// <summary>The point the avatar is currently looking at
        /// (may not stay updated)</summary>
        public LLVector3 LookAt;
        /// <summary>Position avatar client will goto when login to 'home' or during
        /// teleport request to 'home' region.</summary>
        public LLVector3 HomePosition;
        /// <summary>LookAt point saved/restored with HomePosition</summary>
        public LLVector3 HomeLookAt;
        /// <summary>Gets the health of the agent</summary>
        protected float health;
        public float Health
        {
            get { return health; }
        }
        /// <summary>Gets the current balance of the agent</summary>
        protected int balance;
        public int Balance
        {
            get { return balance; }
        }

        private SecondLife Client;
        private TeleportCallback OnBeginTeleport;
        private TeleportStatus TeleportStat;
        private Timer TeleportTimer;
        private bool TeleportTimeout;
        private uint HeightWidthGenCounter;

        /// <summary>
        /// Constructor, aka 'CallBack Central' - Setup callbacks for packets related to our avatar
        /// </summary>
        /// <param name="client"></param>
        public MainAvatar(SecondLife client)
        {
            PacketCallback callback;
            Client = client;
            TeleportMessage = "";

            // Create emtpy vectors for now
            HomeLookAt = HomePosition = Position = LookAt = new LLVector3();
            Rotation = new LLQuaternion();

            // Coarse location callback
            Client.Network.RegisterCallback(PacketType.CoarseLocationUpdate, new PacketCallback(CoarseLocationHandler));

            // Teleport callbacks
            callback = new PacketCallback(TeleportHandler);
            Client.Network.RegisterCallback(PacketType.TeleportStart, callback);
            Client.Network.RegisterCallback(PacketType.TeleportProgress, callback);
            Client.Network.RegisterCallback(PacketType.TeleportFailed, callback);
            Client.Network.RegisterCallback(PacketType.TeleportFinish, callback);

            // Instant Message callback
            Client.Network.RegisterCallback(PacketType.ImprovedInstantMessage, new PacketCallback(InstantMessageHandler));

            // Chat callback
            Client.Network.RegisterCallback(PacketType.ChatFromSimulator, new PacketCallback(ChatHandler));

            TeleportTimer = new Timer(18000);
            TeleportTimer.Elapsed += new ElapsedEventHandler(TeleportTimerEvent);
            TeleportTimeout = false;

            // Movement complete callback
            Client.Network.RegisterCallback(PacketType.AgentMovementComplete, new PacketCallback(MovementCompleteHandler));

            // Health callback
            Client.Network.RegisterCallback(PacketType.HealthMessage, new PacketCallback(HealthHandler));

            // Money callbacks
            callback = new PacketCallback(BalanceHandler);
            Client.Network.RegisterCallback(PacketType.MoneyBalanceReply, callback);
            Client.Network.RegisterCallback(PacketType.MoneySummaryReply, callback);
            Client.Network.RegisterCallback(PacketType.AdjustBalance, callback);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="message"></param>
        public void InstantMessage(LLUUID target, string message)
        {
            InstantMessage(FirstName + " " + LastName, LLUUID.GenerateUUID(), target, message, null, LLUUID.GenerateUUID());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="message"></param>
        /// <param name="IMSessionID"></param>
        public void InstantMessage(LLUUID target, string message, LLUUID IMSessionID)
        {
            InstantMessage(FirstName + " " + LastName, LLUUID.GenerateUUID(), target, message, null, IMSessionID);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fromName"></param>
        /// <param name="sessionID"></param>
        /// <param name="target"></param>
        /// <param name="message"></param>
        /// <param name="conferenceIDs"></param>
        public void InstantMessage(string fromName, LLUUID sessionID, LLUUID target, string message, LLUUID[] conferenceIDs)
        {
            InstantMessage(fromName, sessionID, target, message, conferenceIDs, LLUUID.GenerateUUID());
        }

        /// <summary>
        /// Generate an Instant Message (Full Arguments).
        /// </summary>
        /// <param name="fromName">Client's Avatar</param>
        /// <param name="sessionID">SessionID of current connection to grid</param>
        /// <param name="target">UUID of target Av.</param>
        /// <param name="message">Text Message being sent.</param>
        /// <param name="conferenceIDs"></param>
        /// <param name="IMSessionID"></param>
        /// 
        /// TODO: Have fromName grabbed from elsewhere and remove argument, to prevent inadvertant spoofing.
        /// 
        public void InstantMessage(string fromName, LLUUID sessionID, LLUUID target, string message, 
            LLUUID[] conferenceIDs, LLUUID IMSessionID)
        {
            ImprovedInstantMessagePacket im = new ImprovedInstantMessagePacket();
            im.AgentData.AgentID = this.ID;
            im.AgentData.SessionID = Client.Network.SessionID;
            im.MessageBlock.Dialog = 0;
            im.MessageBlock.FromAgentName = Helpers.StringToField(fromName);
            im.MessageBlock.FromGroup = false;
            im.MessageBlock.ID = IMSessionID;
            im.MessageBlock.Message = Helpers.StringToField(message);
            im.MessageBlock.Offline = 1;
            im.MessageBlock.ToAgentID = target;
            if (conferenceIDs != null && conferenceIDs.Length > 0)
            {
                im.MessageBlock.BinaryBucket = new byte[16 * conferenceIDs.Length];

                for (int i = 0; i < conferenceIDs.Length; ++i)
                {
                    Array.Copy(conferenceIDs[i].Data, 0, im.MessageBlock.BinaryBucket, i * 16, 16);
                }
            }
            else
            {
                im.MessageBlock.BinaryBucket = new byte[0];
            }

            // These fields are mandatory, even if we don't have valid values for them
            im.MessageBlock.Position = new LLVector3();
                //TODO: Allow region id to be correctly set by caller or fetched from Client.*
            im.MessageBlock.RegionID = new LLUUID(); 


            // Send the message
            Client.Network.SendPacket((Packet)im);
        }

        /// <summary>
        /// Conversion type to denote Chat Packet types in an easier-to-understand format
        /// </summary>
        public enum ChatType
        {
            /// <summary>Whispers (5m radius)</summary>
            Whisper = 0,
            /// <summary>Normal chat (10/20m radius) - Why is this here twice?</summary>
            Normal = 1,
            /// <summary>Shouting! (100m radius)</summary>
            Shout = 2,
            /// <summary>Normal chat (10/20m radius) - Why is this here twice?</summary>
            Say = 3,
            /// <summary>Event message when an Avatar has begun to type</summary>
            StartTyping = 4,
            /// <summary>Event message when an Avatar has stopped typing</summary>
            StopTyping = 5
        }

        /// <summary>
        /// Send a Chat message.
        /// </summary>
        /// <param name="message">The Message you're sending out.</param>
        /// <param name="channel">Channel number (0 would be default 'Say' message, other numbers 
        /// denote the equivalent of /# in normal client).</param>
        /// <param name="type">Chat Type, see above.</param>
        public void Chat(string message, int channel, ChatType type)
        {
            ChatFromViewerPacket chat = new ChatFromViewerPacket();
            chat.AgentData.AgentID = this.ID;
            chat.AgentData.SessionID = Client.Network.SessionID;
            chat.ChatData.Channel = channel;
            chat.ChatData.Message = Helpers.StringToField(message);
            chat.ChatData.Type = (byte)type;

            Client.Network.SendPacket((Packet)chat);
        }

        /// <summary>
        /// Set the height and the width of your avatar. This is used to scale
        /// the avatar mesh.
        /// </summary>
        /// <param name="height">New height of the avatar</param>
        /// <param name="width">New width of the avatar</param>
        public void SetHeightWidth(ushort height, ushort width)
        {
            AgentHeightWidthPacket heightwidth = new AgentHeightWidthPacket();
            heightwidth.AgentData.AgentID = Client.Network.AgentID;
            heightwidth.AgentData.SessionID = Client.Network.SessionID;
            heightwidth.AgentData.CircuitCode = Client.Network.CurrentSim.CircuitCode;
            heightwidth.HeightWidthBlock.Height = height;
            heightwidth.HeightWidthBlock.Width = width;
            heightwidth.HeightWidthBlock.GenCounter = HeightWidthGenCounter++;

            Client.Network.SendPacket((Packet)heightwidth);
        }

        /// <summary>
        /// Give Money to destination Avatar
        /// </summary>
        /// <param name="target">UUID of the Target Avatar</param>
        /// <param name="amount">Amount in L$</param>
        /// <param name="description">Reason (optional normally)</param>
        public void GiveMoney(LLUUID target, int amount, string description)
        {
            // 5001 - transaction type for av to av money transfers
            
            GiveMoney(target, amount, description, 5001);
        }

        /// <summary>
        /// Give Money to destionation Object or Avatar
        /// </summary>
        /// <param name="target">UUID of the Target Object/Avatar</param>
        /// <param name="amount">Amount in L$</param>
        /// <param name="description">Reason (Optional normally)</param>
        /// <param name="transactiontype">The type of transaction.  Currently only 5001 is
        /// documented for Av->Av money transfers.</param>
        public void GiveMoney(LLUUID target, int amount, string description, int transactiontype)
        {
            MoneyTransferRequestPacket money = new MoneyTransferRequestPacket();
            money.AgentData.AgentID = this.ID;
            money.AgentData.SessionID = Client.Network.SessionID;
            money.MoneyData.Description = Helpers.StringToField(description);
            money.MoneyData.DestID = target;
            money.MoneyData.SourceID = this.ID;
            money.MoneyData.TransactionType = transactiontype;
            money.MoneyData.AggregatePermInventory = 0; //TODO: whats this?
            money.MoneyData.AggregatePermNextOwner = 0; //TODO: whats this?
            money.MoneyData.Flags = 0; //TODO: whats this?
            money.MoneyData.Amount = amount;

            Client.Network.SendPacket((Packet)money);
        }

        /// <summary>
        /// Use the autopilot sim function to move the avatar to a new position
        /// </summary>
        /// <remarks>The z value is currently not handled properly by the simulator</remarks>
        /// <param name="globalX">Integer value for the global X coordinate to move to</param>
        /// <param name="globalY">Integer value for the global Y coordinate to move to</param>
        /// <param name="z">Floating-point value for the Z coordinate to move to</param>
        /// <example>AutoPilot(252620, 247078, 20.2674);</example>
        public void AutoPilot(ulong globalX, ulong globalY, float z)
        {
            GenericMessagePacket autopilot = new GenericMessagePacket();

            autopilot.AgentData.AgentID = Client.Network.AgentID;
            autopilot.AgentData.SessionID = Client.Network.SessionID;
            autopilot.MethodData.Invoice = new LLUUID();
            autopilot.MethodData.Method = Helpers.StringToField("autopilot");
            autopilot.ParamList = new GenericMessagePacket.ParamListBlock[3];
            autopilot.ParamList[0] = new GenericMessagePacket.ParamListBlock();
            autopilot.ParamList[0].Parameter = Helpers.StringToField(globalX.ToString());
            autopilot.ParamList[1] = new GenericMessagePacket.ParamListBlock();
            autopilot.ParamList[1].Parameter = Helpers.StringToField(globalY.ToString());
            autopilot.ParamList[2] = new GenericMessagePacket.ParamListBlock();
            // TODO: Do we need to prevent z coordinates from being sent in 1.4827e-18 notation?
            autopilot.ParamList[2].Parameter = Helpers.StringToField(z.ToString());

            Client.Network.SendPacket(autopilot);
        }

        /// <summary>
        /// Use the autopilot sim function to move the avatar to a new position
        /// </summary>
        /// <remarks>The z value is currently not handled properly by the simulator</remarks>
        /// <param name="localX">Integer value for the local X coordinate to move to</param>
        /// <param name="localY">Integer value for the local Y coordinate to move to</param>
        /// <param name="z">Floating-point value for the Z coordinate to move to</param>
        /// <example>AutoPilot(252620, 247078, 20.2674);</example>
        public void AutoPilotLocal(int localX, int localY, float z)
        {
            GridRegion gr = Client.Network.CurrentSim.Region.GridRegionData;
            ulong GridCornerX = ((ulong)gr.X * (ulong)256) + (ulong)localX;
            ulong GridCornerY = ((ulong)gr.Y * (ulong)256) + (ulong)localY;
            AutoPilot(GridCornerX, GridCornerY, z);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="regionHandle"></param>
        /// <param name="position"></param>
        /// <param name="tc"></param>
        public void BeginTeleport(ulong regionHandle, LLVector3 position, TeleportCallback tc)
        {
            BeginTeleport(regionHandle, position, new LLVector3(position.X + 1.0f, position.Y, position.Z), tc);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="regionHandle"></param>
        /// <param name="position"></param>
        /// <param name="lookAt"></param>
        /// <param name="tc"></param>
        public void BeginTeleport(ulong regionHandle, LLVector3 position, LLVector3 lookAt, TeleportCallback tc)
        {
            OnBeginTeleport = tc;

            TeleportLocationRequestPacket teleport = new TeleportLocationRequestPacket();
            teleport.AgentData.AgentID = Client.Network.AgentID;
            teleport.AgentData.SessionID = Client.Network.SessionID;
            teleport.Info.LookAt = lookAt;
            teleport.Info.Position = position;
            teleport.Info.RegionHandle = regionHandle;

            Client.Log("Teleporting to region " + regionHandle.ToString(), Helpers.LogLevel.Info);

            Client.Network.SendPacket(teleport);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="regionHandle"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public bool Teleport(ulong regionHandle, LLVector3 position)
        {
            return Teleport(regionHandle, position, new LLVector3(position.X + 1.0f, position.Y, position.Z));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="regionHandle"></param>
        /// <param name="position"></param>
        /// <param name="lookAt"></param>
        /// <returns></returns>
        public bool Teleport(ulong regionHandle, LLVector3 position, LLVector3 lookAt)
        {
            TeleportStat = TeleportStatus.None;

            TeleportLocationRequestPacket teleport = new TeleportLocationRequestPacket();
            teleport.AgentData.AgentID = Client.Network.AgentID;
            teleport.AgentData.SessionID = Client.Network.SessionID;
            teleport.Info.LookAt = lookAt;
            teleport.Info.Position = position;
            
            teleport.Info.RegionHandle = regionHandle;

            Client.Log("Teleporting to region " + regionHandle.ToString(), Helpers.LogLevel.Info);

            // Start the timeout check
            TeleportTimeout = false;
            TeleportTimer.Start();

            Client.Network.SendPacket(teleport);

            while (TeleportStat != TeleportStatus.Failed && TeleportStat != TeleportStatus.Finished && !TeleportTimeout)
            {
                Client.Tick();
            }

            TeleportTimer.Stop();

            if (TeleportTimeout)
            {
                TeleportMessage = "Teleport timed out.";
                TeleportStat = TeleportStatus.Failed;

                if (OnTeleport != null) { OnTeleport(TeleportMessage, TeleportStat); }
            }
            else
            {
                if (OnTeleport != null) { OnTeleport(TeleportMessage, TeleportStat); }
            }

            return (TeleportStat == TeleportStatus.Finished);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="simName"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public bool Teleport(string simName, LLVector3 position)
        {
            //position.Z = 0; //why was this here?
            return Teleport(simName, position, new LLVector3(0, 1.0F, 0));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="simName"></param>
        /// <param name="position"></param>
        /// <param name="lookAt"></param>
        /// <returns></returns>
        public bool Teleport(string simName, LLVector3 position, LLVector3 lookAt)
        {
            int attempts = 0;
            TeleportStat = TeleportStatus.None;

            simName = simName.ToLower();

            GridRegion region = Client.Grid.GetGridRegion(simName);

            if (region != null)
            {
                return Teleport(region.RegionHandle, position, lookAt);
            }
            else
            {
                while (attempts++ < 5)
                {
                    region = Client.Grid.GetGridRegion(simName);

                    if (region != null)
                    {
                        return Teleport(region.RegionHandle, position, lookAt);
                    }
                    else
                    {
                        // Request the region info again
                        Client.Grid.AddSim(simName);
                        
                        System.Threading.Thread.Sleep(1000);
                    }
                }
            }

            if (OnTeleport != null)
            {
                TeleportMessage = "Unable to resolve name: " + simName;
                TeleportStat = TeleportStatus.Failed;
                OnTeleport(TeleportMessage, TeleportStat);
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="simulator"></param>
        public void CompleteAgentMovement(Simulator simulator)
        {
            CompleteAgentMovementPacket move = new CompleteAgentMovementPacket();

            move.AgentData.AgentID = Client.Network.AgentID;
            move.AgentData.SessionID = Client.Network.SessionID;
            move.AgentData.CircuitCode = simulator.CircuitCode;

            Client.Network.SendPacket(move, simulator);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reliable"></param>
        public void UpdateCamera(bool reliable)
        {
            AgentUpdatePacket update = new AgentUpdatePacket();
            update.AgentData.AgentID = Client.Network.AgentID;
            update.AgentData.SessionID = Client.Network.SessionID;
            update.AgentData.State = 0;
            update.AgentData.BodyRotation = new LLQuaternion(0, 0.6519076f, 0, 0);
            update.AgentData.HeadRotation = new LLQuaternion();
            // Semi-sane default values
            update.AgentData.CameraCenter = new LLVector3(9.549901f, 7.033957f, 11.75f);
            update.AgentData.CameraAtAxis = new LLVector3(0.7f, 0.7f, 0);
            update.AgentData.CameraLeftAxis = new LLVector3(-0.7f, 0.7f, 0);
            update.AgentData.CameraUpAxis = new LLVector3(0.1822026f, 0.9828722f, 0);
            update.AgentData.Far = 384;
            update.AgentData.ControlFlags = 0;
            update.AgentData.Flags = 0;
            update.Header.Reliable = reliable;

            Client.Network.SendPacket(update);

            // Send an AgentFOV packet widening our field of vision
            /*AgentFOVPacket fovPacket = new AgentFOVPacket();
            fovPacket.AgentData.AgentID = this.ID;
            fovPacket.AgentData.SessionID = Client.Network.SessionID;
            fovPacket.AgentData.CircuitCode = simulator.CircuitCode;
            fovPacket.FOVBlock.GenCounter = 0;
            fovPacket.FOVBlock.VerticalAngle = 6.28318531f;
            fovPacket.Header.Reliable = true;
            Client.Network.SendPacket((Packet)fovPacket);*/
        }

        /// <summary>
        /// [UNUSED - for now]
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="simulator"></param>
        private void CoarseLocationHandler(Packet packet, Simulator simulator)
        {
            // TODO: This will be useful one day
        }

        /// <summary>
        /// Take an incoming ImprovedInstantMessage packet, auto-parse, and if
        ///   OnInstantMessage is defined call that with the appropriate arguments.
        /// </summary>
        /// <param name="packet">Incoming ImprovedInstantMessagePacket</param>
        /// <param name="simulator">[UNUSED]</param>
        private void InstantMessageHandler(Packet packet, Simulator simulator)
        {
            if (packet.Type == PacketType.ImprovedInstantMessage)
            {
                ImprovedInstantMessagePacket im = (ImprovedInstantMessagePacket)packet;
                
                if (OnInstantMessage != null)
                {
                    OnInstantMessage(
                        im.AgentData.AgentID
                        , Helpers.FieldToString(im.MessageBlock.FromAgentName),
                        im.MessageBlock.ToAgentID
                        , im.MessageBlock.ParentEstateID
                        , im.MessageBlock.RegionID
                        , im.MessageBlock.Position
                        , im.MessageBlock.Dialog
                        , im.MessageBlock.FromGroup
                        , im.MessageBlock.ID
                        , new DateTime(im.MessageBlock.Timestamp)
                        , Helpers.FieldToString(im.MessageBlock.Message)
                        , im.MessageBlock.Offline
                        , im.MessageBlock.BinaryBucket
                        );
                }
            }
        }

        /// <summary>
        /// Take an incoming Chat packet, auto-parse, and if OnChat is defined call 
        ///   that with the appropriate arguments.
        /// </summary>
        /// <param name="packet">Incoming ChatFromSimulatorPacket</param>
        /// <param name="simulator">[UNUSED]</param>
        private void ChatHandler(Packet packet, Simulator simulator)
        {
            if (packet.Type == PacketType.ChatFromSimulator)
            {
                ChatFromSimulatorPacket chat = (ChatFromSimulatorPacket)packet;

                if (OnChat != null)
                {
                    OnChat(Helpers.FieldToString(chat.ChatData.Message), chat.ChatData.Audible, chat.ChatData.ChatType, 
                        chat.ChatData.SourceType, Helpers.FieldToString(chat.ChatData.FromName), chat.ChatData.SourceID);
                }
            }
        }

        /// <summary>
        /// Update client's Position and LookAt from incoming packet
        /// </summary>
        /// <param name="packet">Incoming AgentMovementCompletePacket</param>
        /// <param name="simulator">[UNUSED]</param>
        private void MovementCompleteHandler(Packet packet, Simulator simulator)
        {
            AgentMovementCompletePacket movement = (AgentMovementCompletePacket)packet;

            this.Position = movement.Data.Position;
            this.LookAt = movement.Data.LookAt;
        }

        /// <summary>
        /// Update Client Avatar's health via incoming packet
        /// </summary>
        /// <param name="packet">Incoming HealthMessagePacket</param>
        /// <param name="simulator">[UNUSED]</param>
        private void HealthHandler(Packet packet, Simulator simulator)
        {
            health = ((HealthMessagePacket)packet).HealthData.Health;
        }

        /// <summary>
        /// Update Client Avatar's L$ balance from incoming packet
        /// </summary>
        /// <param name="packet">Incoming MoneyBalanceReplyPacket</param>
        /// <param name="simulator">[UNUSED]</param>
        private void BalanceHandler(Packet packet, Simulator simulator)
        {
            if (packet.Type == PacketType.MoneyBalanceReply)
            {
                balance = ((MoneyBalanceReplyPacket)packet).MoneyData.MoneyBalance;
            }
            else if (packet.Type == PacketType.MoneySummaryReply)
            {
                balance = ((MoneySummaryReplyPacket)packet).MoneyData.Balance;
            }
            else if (packet.Type == PacketType.AdjustBalance)
            {
                balance += ((AdjustBalancePacket)packet).AgentData.Delta;
            }

            if (OnBalanceUpdated != null)
            {
                OnBalanceUpdated(balance);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="simulator"></param>
        private void TeleportHandler(Packet packet, Simulator simulator)
        {
            if (packet.Type == PacketType.TeleportStart)
            {
                TeleportMessage = "Teleport started";
                TeleportStat = TeleportStatus.Start;

                if (OnBeginTeleport != null)
                {
                    OnBeginTeleport(TeleportMessage, TeleportStat);
                }
            }
            else if (packet.Type == PacketType.TeleportProgress)
            {
                TeleportMessage = Helpers.FieldToString(((TeleportProgressPacket)packet).Info.Message);
                TeleportStat = TeleportStatus.Progress;

                if (OnBeginTeleport != null)
                {
                    OnBeginTeleport(TeleportMessage, TeleportStat);
                }
            }
            else if (packet.Type == PacketType.TeleportFailed)
            {
                TeleportMessage = Helpers.FieldToString(((TeleportFailedPacket)packet).Info.Reason);
                TeleportStat = TeleportStatus.Failed;

                if (OnBeginTeleport != null)
                {
                    OnBeginTeleport(TeleportMessage, TeleportStat);
                }

                OnBeginTeleport = null;
            }
            else if (packet.Type == PacketType.TeleportFinish)
            {
                TeleportFinishPacket finish = (TeleportFinishPacket)packet;

                // Connect to the new sim
                Simulator sim = Client.Network.Connect(new IPAddress((long)finish.Info.SimIP), finish.Info.SimPort, 
                    simulator.CircuitCode, true);
                
                if ( sim != null)
                {
                    TeleportMessage = "Teleport finished";
                    TeleportStat = TeleportStatus.Finished;

                    // Move the avatar in to the new sim
                    CompleteAgentMovementPacket move = new CompleteAgentMovementPacket();

                    move.AgentData.AgentID = Client.Network.AgentID;
                    move.AgentData.SessionID = Client.Network.SessionID;
                    move.AgentData.CircuitCode = simulator.CircuitCode;

                    Client.Network.SendPacket((Packet)move);

                    Client.DebugLog(move.ToString());

                    Client.Log("Moved to new sim " + Client.Network.CurrentSim.Region.Name + "(" + 
                        Client.Network.CurrentSim.IPEndPoint.ToString() + ")",
                        Helpers.LogLevel.Info);

                    if (OnBeginTeleport != null)
                    {
                        OnBeginTeleport(TeleportMessage, TeleportStat);
                    }
                    else
                    {
                        // Sleep a little while so we can collect parcel information
                        System.Threading.Thread.Sleep(1000);
                    }
                }
                else
                {
                    TeleportMessage = "Failed to connect to the new sim after a teleport";
                    TeleportStat = TeleportStatus.Failed;

                    Client.Log(TeleportMessage, Helpers.LogLevel.Warning);

                    if (OnBeginTeleport != null)
                    {
                        OnBeginTeleport(TeleportMessage, TeleportStat);
                    }
                }

                OnBeginTeleport = null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="ea"></param>
        private void TeleportTimerEvent(object source, System.Timers.ElapsedEventArgs ea)
        {
            TeleportTimeout = true;
        }
    }
}
