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
using System.Collections.Generic;
using libsecondlife.Packets;

namespace libsecondlife
{
    /// <summary>
    /// A parcel retrieved from the dataserver such as results from the 
    /// "For-Sale" listings
    /// </summary>
    public class DirectoryParcel
    {
        /// <summary></summary>
        public LLUUID ID;
        /// <summary></summary>
        public LLUUID OwnerID;
        /// <summary></summary>
        public LLUUID SnapshotID;
        /// <summary></summary>
        public ulong RegionHandle;
        /// <summary></summary>
        public string Name;
        /// <summary></summary>
        public string SimName;
        /// <summary></summary>
        public string Desc;
        /// <summary></summary>
        public int SalePrice;
        /// <summary></summary>
        public int ActualArea;
        /// <summary></summary>
        public LLVector3 GlobalPosition;
        /// <summary></summary>
        public LLVector3 SimPosition;
        /// <summary></summary>
        public float Dwell;

        /// <summary>
        /// 
        /// </summary>
        public DirectoryParcel()
        {
            GlobalPosition = LLVector3.Zero;
            SimPosition = LLVector3.Zero;
        }
    }

    /// <summary>
    /// Parcel information retrieved from a simulator
    /// </summary>
    public class Parcel
    {
        /// <summary></summary>
        public int RequestResult;
        /// <summary></summary>
        public int SequenceID;
        /// <summary></summary>
        public bool SnapSelection;
        /// <summary></summary>
        public int SelfCount;
        /// <summary></summary>
        public int OtherCount;
        /// <summary></summary>
        public int PublicCount;
        /// <summary></summary>
        public int LocalID;
        /// <summary>Key of land owner</summary>
        public LLUUID OwnerID;
        /// <summary>Is the land group owned</summary>
        public bool IsGroupOwned;
        /// <summary></summary>
        public uint AuctionID;
        /// <summary>Presumably for first land</summary>
//        public bool ReservedNewbie; // FIXME FIXME FIXME
        /// <summary>Date land was claimed</summary>
        public int ClaimDate;
        /// <summary></summary>
        public int ClaimPrice;
        /// <summary></summary>
        public int RentPrice;
        /// <summary></summary>
        public LLVector3 AABBMin;
        /// <summary></summary>
        public LLVector3 AABBMax;
        /// <summary>Bitmap describing land layout in 4x4m squares across the entire region</summary>
        public byte[] Bitmap;
        /// <summary>Total land area</summary>
        public int Area;
        /// <summary></summary>
        public byte Status;
        /// <summary>Max objects across region</summary>
        public int SimWideMaxObjects;
        /// <summary>Total objects across region</summary>
        public int SimWideTotalObjects;
        /// <summary>Max objects for parcel</summary>
        public int MaxObjects;
        /// <summary>Total objects in parcel</summary>
        public int TotalObjects;
        /// <summary>Total objects for owner</summary>
        public int OwnerObjects;
        /// <summary>Total objects for group</summary>
        public int GroupObjects;
        /// <summary>Total for other objects</summary>
        public int OtherObjects;
        /// <summary></summary>
        public float ParcelObjectBonus;
        /// <summary></summary>
        public int OtherCleanTime;
        /// <summary></summary>
        public uint ParcelFlags;
        /// <summary></summary>
        public int SalePrice;
        /// <summary>Parcel Name</summary>
        public string Name;
        /// <summary>Parcel Description</summary>
        public string Desc;
        /// <summary>URL For Music Stream</summary>
        public string MusicURL;
        /// <summary>URL For other Media</summary>
        public string MediaURL;
        /// <summary>Key to Picture for Media Placeholder</summary>
        public LLUUID MediaID;
        /// <summary></summary>
        public byte MediaAutoScale;
        /// <summary></summary>
        public LLUUID GroupID;
        /// <summary>Price for a temporary pass</summary>
        public int PassPrice;
        /// <summary>How long is pass valid for</summary>
        public float PassHours;
        /// <summary></summary>
        public byte Category;
        /// <summary>Key of authorized buyer</summary>
        public LLUUID AuthBuyerID;
        /// <summary>Key of parcel snapshot</summary>
        public LLUUID SnapshotID;
        /// <summary></summary>
        public LLVector3 UserLocation;
        /// <summary></summary>
        public LLVector3 UserLookAt;
        /// <summary></summary>
        public byte LandingType;
        /// <summary></summary>
        public float Dwell;

        // Using Sim instead of Region since it references both
        private Simulator Sim;

        private void init()
        {
            OwnerID = LLUUID.Zero;
            AABBMin = LLVector3.Zero;
            AABBMax = LLVector3.Zero;
            Bitmap = new byte[512];
            MediaID = LLUUID.Zero;
            GroupID = LLUUID.Zero;
            AuthBuyerID = LLUUID.Zero;
            SnapshotID = LLUUID.Zero;
            UserLocation = LLVector3.Zero;
            UserLookAt = LLVector3.Zero;
        }

        /// <summary>
        /// 
        /// </summary>
        public Parcel()
        {
            init();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="simulator"></param>
        public Parcel(Simulator simulator)
        {
            Sim = simulator;
            init();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        public void GetDwell(SecondLife client)
        {
            ParcelDwellRequestPacket request = new ParcelDwellRequestPacket();
            request.AgentData.AgentID = client.Network.AgentID;
            request.AgentData.SessionID = client.Network.SessionID;
            request.Data.LocalID = LocalID;
            request.Data.ParcelID = LLUUID.Zero;

            client.Network.SendPacket((Packet)request, Sim);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="forGroup"></param>
        /// <param name="groupID"></param>
        /// <returns></returns>
        public bool Buy(SecondLife client, bool forGroup, LLUUID groupID, bool removeContribution)
        {
            ParcelBuyPacket request = new ParcelBuyPacket();

            request.AgentData.AgentID = client.Network.AgentID;
            request.AgentData.SessionID = client.Network.SessionID;

            request.Data.Final = true;
            request.Data.GroupID = groupID;
            request.Data.LocalID = this.LocalID;
            request.Data.IsGroupOwned = forGroup;
            request.Data.RemoveContribution = removeContribution;

            client.Network.SendPacket((Packet)request, Sim);
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool Reclaim(SecondLife client)
        {
            ParcelReclaimPacket request = new ParcelReclaimPacket();
            request.AgentData.AgentID = client.Network.AgentID;
            request.AgentData.SessionID = client.Network.SessionID;

            request.Data.LocalID = this.LocalID;

            client.Network.SendPacket((Packet)request, Sim);
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="groupID"></param>
        /// <returns></returns>
        public bool Deed(SecondLife client, LLUUID groupID)
        {
            ParcelDeedToGroupPacket request = new ParcelDeedToGroupPacket();
            request.AgentData.AgentID = client.Network.AgentID;
            request.AgentData.SessionID = client.Network.SessionID;

            request.Data.LocalID = this.LocalID;
            request.Data.GroupID = groupID;

            client.Network.SendPacket((Packet)request, Sim);
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        public void Update(SecondLife client)
        {
            ParcelPropertiesUpdatePacket request = new ParcelPropertiesUpdatePacket();

            request.AgentData.AgentID = client.Network.AgentID;
            request.AgentData.SessionID = client.Network.SessionID;

            request.ParcelData.Flags = 0xFFFFFFFF; // TODO: Probably very important
            request.ParcelData.LocalID = this.LocalID;

            request.ParcelData.AuthBuyerID = this.AuthBuyerID;
            request.ParcelData.Category = this.Category;
            request.ParcelData.Desc = Helpers.StringToField(this.Desc);
            request.ParcelData.GroupID = this.GroupID;
            request.ParcelData.LandingType = this.LandingType;
            request.ParcelData.MediaAutoScale = this.MediaAutoScale;
            request.ParcelData.MediaID = this.MediaID;
            request.ParcelData.MediaURL = Helpers.StringToField(this.MediaURL);
            request.ParcelData.MusicURL = Helpers.StringToField(this.MusicURL);
            request.ParcelData.Name = Helpers.StringToField(this.Name);
            request.ParcelData.ParcelFlags = this.ParcelFlags;
            request.ParcelData.PassHours = this.PassHours;
            request.ParcelData.PassPrice = this.PassPrice;
            request.ParcelData.SalePrice = this.SalePrice;
            request.ParcelData.SnapshotID = this.SnapshotID;
            request.ParcelData.UserLocation = this.UserLocation;
            request.ParcelData.UserLookAt = this.UserLookAt;

            client.Network.SendPacket((Packet)request, Sim);
            //Packet updatePacket = Packets.Parcel.ParcelPropertiesUpdate(client.Protocol, client.Avatar.ID, client.Network.SessionID, this);
            //Sim.SendPacket(updatePacket, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="returnType"></param>
        public void ReturnObjects(SecondLife client, uint returnType)
        {
            // TODO: ENUM for returnType

            ParcelReturnObjectsPacket request = new ParcelReturnObjectsPacket();
            request.AgentData.AgentID = client.Network.AgentID;
            request.AgentData.SessionID = client.Network.SessionID;

            request.ParcelData.LocalID = this.LocalID;
            request.ParcelData.ReturnType = returnType;

            // TODO: Handling of TaskIDs and OwnerIDs
            request.OwnerIDs = new ParcelReturnObjectsPacket.OwnerIDsBlock[0];
            request.TaskIDs = new ParcelReturnObjectsPacket.TaskIDsBlock[1];

            client.Network.SendPacket((Packet)request, Sim);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class ParcelManager
    {
        private SecondLife Client;
//        private bool ReservedNewbie; // FIXME FIXME FIXME
//        private bool ForSale;
//        private bool Auction;
        private bool Finished;
        private Timer DirLandTimer;
        private bool DirLandTimeout;
        private bool ParcelInfoTimeout;
        private DirectoryParcel ParcelInfoParcel;
        private List<DirectoryParcel> ParcelsForSale;

        /// <summary>
        /// Parcel (subdivided simulator lots) Subsystem
        /// </summary>
        /// <param name="client"></param>
        public ParcelManager(SecondLife client)
        {
            Client = client;
            ParcelsForSale = new List<DirectoryParcel>();

            // Setup the timer
            DirLandTimer = new Timer(8000);
            DirLandTimer.Elapsed += new ElapsedEventHandler(DirLandTimerEvent);
            DirLandTimeout = false;

            // Setup the callbacks
            Client.Network.RegisterCallback(PacketType.DirLandReply, new NetworkManager.PacketCallback(DirLandReplyHandler));
            Client.Network.RegisterCallback(PacketType.ParcelInfoReply, new NetworkManager.PacketCallback(ParcelInfoReplyHandler));
            Client.Network.RegisterCallback(PacketType.ParcelProperties, new NetworkManager.PacketCallback(ParcelPropertiesHandler));
            Client.Network.RegisterCallback(PacketType.ParcelDwellReply, new NetworkManager.PacketCallback(ParcelDwellReplyHandler));

            ParcelInfoParcel = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="simulator"></param>
        public void ParcelDwellReplyHandler(Packet packet, Simulator simulator)
        {
            ParcelDwellReplyPacket dwell = (ParcelDwellReplyPacket)packet;

            if (dwell.Data.Dwell != 0.0F && simulator.Region.Parcels.ContainsKey(dwell.Data.LocalID))
            {
                ((Parcel)simulator.Region.Parcels[dwell.Data.LocalID]).Dwell = dwell.Data.Dwell;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parcel"></param>
        /// <returns></returns>
        public bool RequestParcelInfo(DirectoryParcel parcel)
        {
            int attempts = 0;

        Beginning:
            if (attempts++ > 3) { return false; }

            Finished = false;
            ParcelInfoTimeout = false;
            ParcelInfoParcel = parcel;

        //    // Setup the timer
        //    Timer ParcelInfoTimer = new Timer(5000);
        //    ParcelInfoTimer.Elapsed += new ElapsedEventHandler(ParcelInfoTimerEvent);
        //    ParcelInfoTimeout = false;

        //    // Build the ParcelInfoRequest packet
        //    ParcelInfoRequestPacket request = new ParcelInfoRequestPacket();
        //    request.AgentData.AgentID = Client.Network.AgentID;
        //    request.AgentData.SessionID = Client.Network.SessionID;
        //    request.Data.ParcelID = parcel.ID;

        //    // Start the timer
        //    ParcelInfoTimer.Start();

        //    Client.Network.SendPacket((Packet)request);

            while (!Finished)
            {
                // FIXME: This can easily cause an infinite loop
                if (ParcelInfoTimeout) { goto Beginning; }

                Client.Tick();
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reservedNewbie"></param>
        /// <param name="forSale"></param>
        /// <param name="auction"></param>
        /// <returns></returns>
        public int DirLandRequest(bool reservedNewbie, bool forSale, bool auction)
        {
            // Set the class-wide variables so the callback has them
//            ReservedNewbie = reservedNewbie; // FIXME FIXME FIXME
//            ForSale = forSale;
//            Auction = auction;

            // Clear the list
            ParcelsForSale.Clear();

            // Start the timer
            DirLandTimeout = false;
            DirLandTimer.Start();

            DirLandQueryPacket query = new DirLandQueryPacket();
            query.AgentData.AgentID = Client.Network.AgentID;
            query.AgentData.SessionID = Client.Network.SessionID;
//            query.QueryData.Auction = auction; // FIXME FIXME FIXME
//            query.QueryData.ForSale = forSale;
            query.QueryData.QueryFlags = 0;
            query.QueryData.QueryID = LLUUID.Random();
//            query.QueryData.ReservedNewbie = reservedNewbie; // FIXME FIXME FIXME

            Client.Network.SendPacket((Packet)query);

            while (!DirLandTimeout)
            {
                Client.Tick();
            }

            // Make sure the timer is actually stopped
            DirLandTimer.Stop();

            return ParcelsForSale.Count;
        }

        private void ParcelPropertiesHandler(Packet packet, Simulator simulator)
        {
            ParcelPropertiesPacket properties = (ParcelPropertiesPacket)packet;

            byte[] Bitmap = properties.ParcelData.Bitmap;
            int LocalID = properties.ParcelData.LocalID;

            // Mark this area as downloaded
            int x, y, index, subindex;
            byte val;

            for (x = 0; x < 64; x++)
            {
                for (y = 0; y < 64; y++)
                {
                    if (simulator.Region.ParcelMarked[y, x] == 0)
                    {
                        index = ((x * 64) + y);
                        subindex = index % 8;
                        index /= 8;

                        val = Bitmap[index];

                        simulator.Region.ParcelMarked[y, x] = ((val >> subindex) & 1) == 1 ? LocalID : 0;
                    }
                }
            }

            // Fire off the next request, if we are downloading the whole sim
            bool hasTriggered = false;
            if (simulator.Region.ParcelDownloading)
            {
                for (x = 0; x < 64; x++)
                {
                    for (y = 0; y < 64; y++)
                    {
                        if (simulator.Region.ParcelMarked[x, y] == 0)
                        {
                            ParcelPropertiesRequestPacket tPacket = new ParcelPropertiesRequestPacket();
                            tPacket.AgentData.AgentID = Client.Network.AgentID;
                            tPacket.AgentData.SessionID = Client.Network.SessionID;
                            tPacket.ParcelData.SequenceID = -10000;
                            tPacket.ParcelData.West = (x * 4.0f);
                            tPacket.ParcelData.South = (y * 4.0f);
                            tPacket.ParcelData.East = (x * 4.0f) + 4.0f;
                            tPacket.ParcelData.North = (y * 4.0f) + 4.0f;

                            Client.Network.SendPacket(tPacket, simulator);

                            hasTriggered = true;

                            goto exit;
                        }
                    }
                }
            exit:
                ;
            }

            // This map is complete, fire callback
            if (!hasTriggered)
            {
                simulator.Region.FilledParcels();
            }

            Parcel parcel = null;

            lock (simulator.Region.Parcels)
            {
                if (!simulator.Region.Parcels.ContainsKey(LocalID))
                {
                    simulator.Region.Parcels[LocalID] = new Parcel(simulator);
                }

                parcel = simulator.Region.Parcels[LocalID];
            }

            // Save this parcels data
            // TODO: Lots of values are not being stored, Parcel needs to be expanded to take all the data.
            // August2006:  God help me should I have to type this out again... argh.
            // October2006: I really shouldnt have typed that.
            parcel.RequestResult       = properties.ParcelData.RequestResult;
            parcel.SequenceID          = properties.ParcelData.SequenceID;
            parcel.SnapSelection       = properties.ParcelData.SnapSelection;
            parcel.SelfCount           = properties.ParcelData.SelfCount;
            parcel.OtherCount          = properties.ParcelData.OtherCount;
            parcel.PublicCount         = properties.ParcelData.PublicCount;
            parcel.LocalID             = LocalID;
            parcel.OwnerID             = properties.ParcelData.OwnerID;
            parcel.IsGroupOwned        = properties.ParcelData.IsGroupOwned;
            parcel.AuctionID           = properties.ParcelData.AuctionID;
//            parcel.ReservedNewbie      = properties.ParcelData.ReservedNewbie; // FIXME FIXME FIXME
            parcel.ClaimDate           = properties.ParcelData.ClaimDate;
            parcel.ClaimPrice          = properties.ParcelData.ClaimPrice;
            parcel.RentPrice           = properties.ParcelData.RentPrice;
            parcel.AABBMin             = properties.ParcelData.AABBMin;
            parcel.AABBMax             = properties.ParcelData.AABBMax;
            parcel.Bitmap              = properties.ParcelData.Bitmap;
            parcel.Area                = properties.ParcelData.Area;
            parcel.Status              = properties.ParcelData.Status;
            parcel.SimWideMaxObjects   = properties.ParcelData.SimWideMaxPrims;
            parcel.SimWideTotalObjects = properties.ParcelData.SimWideTotalPrims;
            parcel.MaxObjects          = properties.ParcelData.MaxPrims;
            parcel.TotalObjects        = properties.ParcelData.TotalPrims;
            parcel.OwnerObjects        = properties.ParcelData.OwnerPrims;
            parcel.GroupObjects        = properties.ParcelData.GroupPrims;
            parcel.OtherObjects        = properties.ParcelData.OtherPrims;
            parcel.ParcelObjectBonus   = properties.ParcelData.ParcelPrimBonus;
            parcel.OtherCleanTime      = properties.ParcelData.OtherCleanTime;
            parcel.ParcelFlags         = properties.ParcelData.ParcelFlags;
            parcel.SalePrice           = properties.ParcelData.SalePrice;
            parcel.Name                = Helpers.FieldToString(properties.ParcelData.Name);
            parcel.Desc                = Helpers.FieldToString(properties.ParcelData.Desc);
            parcel.MusicURL            = Helpers.FieldToString(properties.ParcelData.MusicURL);
            parcel.MediaURL            = Helpers.FieldToString(properties.ParcelData.MediaURL);
            parcel.MediaID             = properties.ParcelData.MediaID;
            parcel.MediaAutoScale      = properties.ParcelData.MediaAutoScale;
            parcel.GroupID             = properties.ParcelData.GroupID;
            parcel.PassPrice           = properties.ParcelData.PassPrice;
            parcel.PassHours           = properties.ParcelData.PassHours;
            parcel.Category            = properties.ParcelData.Category;
            parcel.AuthBuyerID         = properties.ParcelData.AuthBuyerID;
            parcel.SnapshotID          = properties.ParcelData.SnapshotID;
            parcel.UserLocation        = properties.ParcelData.UserLocation;
            parcel.UserLookAt          = properties.ParcelData.UserLookAt;
            parcel.LandingType         = properties.ParcelData.LandingType;
        }

        private void ParcelInfoReplyHandler(Packet packet, Simulator simulator)
        {
            if (ParcelInfoParcel != null)
            {
                ParcelInfoReplyPacket reply = (ParcelInfoReplyPacket)packet;

                if (!reply.Data.ParcelID.Equals(ParcelInfoParcel.ID))
                {
                    Client.Log("Received a ParcelInfoReply for " + reply.Data.ParcelID.ToString() +
                            ", looking for " + ParcelInfoParcel.ID.ToString(), Helpers.LogLevel.Warning);

                    // Build and resend the ParcelInfoRequest packet
                    ParcelInfoRequestPacket request = new ParcelInfoRequestPacket();
                    request.AgentData.AgentID = Client.Network.AgentID;
                    request.AgentData.SessionID = Client.Network.SessionID;
                    request.Data.ParcelID = ParcelInfoParcel.ID;

                    Client.Network.SendPacket(request);

                    return;
                }

                ParcelInfoParcel.SimName = Helpers.FieldToString(reply.Data.SimName);
                ParcelInfoParcel.ActualArea = reply.Data.ActualArea;
                ParcelInfoParcel.GlobalPosition.X = reply.Data.GlobalX;
                ParcelInfoParcel.GlobalPosition.Y = reply.Data.GlobalY;
                ParcelInfoParcel.GlobalPosition.Z = reply.Data.GlobalZ;
                ParcelInfoParcel.Name = Helpers.FieldToString(reply.Data.Name);
                ParcelInfoParcel.Desc = Helpers.FieldToString(reply.Data.Desc);
                ParcelInfoParcel.SalePrice = reply.Data.SalePrice;
                ParcelInfoParcel.OwnerID = reply.Data.OwnerID;
                ParcelInfoParcel.SnapshotID = reply.Data.SnapshotID;
                ParcelInfoParcel.Dwell = reply.Data.Dwell;

                // Get RegionHandle from GlobalX/GlobalY
                uint handleX = (uint)Math.Floor(ParcelInfoParcel.GlobalPosition.X / 256.0F);
                handleX *= 256;
                uint handleY = (uint)Math.Floor(ParcelInfoParcel.GlobalPosition.Y / 256.0F);
                handleY *= 256;
                // FIXME: Helpers function needed
                //ParcelInfoParcel.RegionHandle = new U64(handleX, handleY);

                // Get SimPosition from GlobalX/GlobalY and RegionHandle
                ParcelInfoParcel.SimPosition.X = ParcelInfoParcel.GlobalPosition.X - (float)handleX;
                ParcelInfoParcel.SimPosition.Y = ParcelInfoParcel.GlobalPosition.Y - (float)handleY;
                ParcelInfoParcel.SimPosition.Z = ParcelInfoParcel.GlobalPosition.Z;

                Finished = true;
            }
        }

        //private void ParcelInfoTimerEvent(object source, System.Timers.ElapsedEventArgs ea)
        //{
        //    ParcelInfoTimeout = true;
        //}

        private void DirLandReplyHandler(Packet packet, Simulator simulator)
        {
            if (!DirLandTimeout)
            {
                // Reset the timer
                DirLandTimer.Stop();
                DirLandTimer.Start();

                DirLandReplyPacket reply = (DirLandReplyPacket)packet;

                foreach (DirLandReplyPacket.QueryRepliesBlock block in reply.QueryReplies)
                {
//                    if (block.ReservedNewbie == ReservedNewbie &&
//                        block.Auction == Auction &&
//                        block.ForSale == ForSale) // FIXME FIXME FIXME
                    {
                        DirectoryParcel parcel = new DirectoryParcel();

                        parcel.ActualArea = block.ActualArea;
                        parcel.ID = block.ParcelID;
                        parcel.Name = Helpers.FieldToString(block.Name);
                        parcel.SalePrice = block.SalePrice;

                        ParcelsForSale.Add(parcel);
                    }
                }
            }
            else
            {
                Client.Log("Received a DirLandReply after the timeout, ignoring", Helpers.LogLevel.Warning);
            }
        }

        private void DirLandTimerEvent(object source, System.Timers.ElapsedEventArgs ea)
        {
            DirLandTimer.Stop();
            DirLandTimeout = true;
        }
    }
}
