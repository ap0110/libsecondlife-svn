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
using System.Collections;

using libsecondlife;

using libsecondlife.InventorySystem;

using libsecondlife.Packets;

namespace libsecondlife.AssetSystem
{
	/// <summary>
	/// Summary description for AssetManager.
	/// </summary>
	public class AssetManager
	{
		public const int SINK_FEE_IMAGE = 1;

		private SecondLife slClient;

		private Hashtable htUploadRequests = new Hashtable();
		private Hashtable htDownloadRequests = new Hashtable();

		private class TransferRequest
		{
			public bool Completed;
			public bool Status;
			public string StatusMsg;

			public int Size;
			public int Received;
			public uint LastPacket;
			public byte[] AssetData;
		}

        /// <summary>
        /// </summary>
        /// <param name="client"></param>
        internal AssetManager(SecondLife client)
		{
			slClient = client;

			// Used to upload small assets, or as an initial start packet for large transfers
			PacketCallback AssetUploadCompleteCallback = new PacketCallback(AssetUploadCompleteCallbackHandler);
            slClient.Network.RegisterCallback(PacketType.AssetUploadComplete, AssetUploadCompleteCallback);

			// Transfer Packets for downloading large assets		
			PacketCallback TransferInfoCallback = new PacketCallback(TransferInfoCallbackHandler);
            slClient.Network.RegisterCallback(PacketType.TransferInfo, TransferInfoCallback);

			PacketCallback TransferPacketCallback = new PacketCallback(TransferPacketCallbackHandler);
            slClient.Network.RegisterCallback(PacketType.TransferPacket, TransferPacketCallback);

			// XFer packets for uploading large assets
			PacketCallback ConfirmXferPacketCallback = new PacketCallback(ConfirmXferPacketCallbackHandler);
            slClient.Network.RegisterCallback(PacketType.ConfirmXferPacket, ConfirmXferPacketCallback);
			
			PacketCallback RequestXferCallback = new PacketCallback(RequestXferCallbackHandler);
            slClient.Network.RegisterCallback(PacketType.RequestXfer, RequestXferCallback);
			
		}


        /// <summary>
        /// Handle the appropriate sink fee assoiacted with an asset upload
        /// </summary>
        /// <param name="sinkType"></param>
        public void SinkFee(int sinkType)
		{
			switch( sinkType )
			{
				case SINK_FEE_IMAGE:
					slClient.Avatar.GiveMoney( new LLUUID(), 10, "Image Upload" );
					break;
				default:
					throw new Exception("AssetManager: Unknown sinktype (" + sinkType + ")");
			}
		}

        /// <summary>
        /// Upload an asset to SecondLife
        /// </summary>
        /// <param name="sinkType"></param>
        public void UploadAsset(Asset asset)
		{
			Packet packet;
			TransferRequest tr = new TransferRequest();
			tr.Completed = false;
			htUploadRequests[asset.AssetID] = tr;

			if( asset.AssetData.Length > 500 )
			{
				packet = AssetPacketHelpers.AssetUploadRequestHeaderOnly(asset);
				slClient.Network.SendPacket(packet);
				Console.WriteLine(packet);
				tr.AssetData = asset.AssetData;
			} 
			else 
			{
                packet = AssetPacketHelpers.AssetUploadRequest(asset);
				slClient.Network.SendPacket(packet);
				Console.WriteLine(packet);
			}

			while( tr.Completed == false )
			{
				slClient.Tick();
			}

			if( tr.Status == false )
			{
				throw new Exception( tr.StatusMsg );
			} else {
				if( asset.Type == Asset.ASSET_TYPE_IMAGE )
				{
					SinkFee( SINK_FEE_IMAGE );
				}
			}
		}

        /// <summary>
        /// Get the Asset data for an item
        /// </summary>
        /// <param name="item"></param>
		public void GetInventoryAsset( InventoryItem item )
		{
			LLUUID TransferID = LLUUID.GenerateUUID();

			TransferRequest tr = new TransferRequest();
			tr.Completed  = false;
			tr.Size		  = int.MaxValue; // Number of bytes expected
			tr.Received   = 0; // Number of bytes received
			tr.LastPacket = Helpers.GetUnixTime(); // last time we recevied a packet for this request

			htDownloadRequests[TransferID] = tr;

			Packet packet = AssetPacketHelpers.TransferRequest(slClient.Network.SessionID, slClient.Network.AgentID, TransferID, item );
			slClient.Network.SendPacket(packet);

			while( tr.Completed == false )
			{
				slClient.Tick();
			}

			item.SetAssetData( tr.AssetData );
		}


        /// <summary>
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="simulator"></param>
        public void AssetUploadCompleteCallbackHandler(Packet packet, Simulator simulator)
		{
            Packets.AssetUploadCompletePacket reply = (AssetUploadCompletePacket)packet;

            LLUUID AssetID = reply.AssetBlock.UUID;
            bool Success = reply.AssetBlock.Success;

            // Lookup the request for this packet, and mark it success or failure
            TransferRequest tr = (TransferRequest)htUploadRequests[AssetID];
			if( Success )
			{
				tr.Completed = true;
				tr.Status    = true;
				tr.StatusMsg = "Success";
			} 
			else 
			{
				tr.Completed = true;
				tr.Status    = false;
				tr.StatusMsg = "Server returned failed";
			}
		}

        /// <summary>
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="simulator"></param>
        public void TransferInfoCallbackHandler(Packet packet, Simulator simulator)
		{
            TransferInfoPacket reply = (TransferInfoPacket)packet;
			
            LLUUID TransferID = reply.TransferInfo.TransferID;
            int Size = reply.TransferInfo.Size;
            int Status = reply.TransferInfo.Status;

            // Lookup the request for this packet
			TransferRequest tr = (TransferRequest)htDownloadRequests[TransferID];
			if( tr == null )
			{
				return;
			}

            // Mark it as either not found or update the request information
            if (Status == -2)
			{
				tr.Completed = true;
				tr.Status    = false;
				tr.StatusMsg = "Asset Status -2 :: Likely Status Not Found";

				tr.Size = 1;
				tr.AssetData = new byte[1];

			} 
			else 
			{
				tr.Size = Size;
				tr.AssetData = new byte[Size];
			}
		}

        /// <summary>
        /// Transfer asset data
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="simulator"></param>
        public void TransferPacketCallbackHandler(Packet packet, Simulator simulator)
		{
            TransferPacketPacket reply = (TransferPacketPacket)packet;

            LLUUID TransferID = reply.TransferData.TransferID;
			byte[] Data       = reply.TransferData.Data;


			// Append data to data received.
			TransferRequest tr = (TransferRequest)htDownloadRequests[TransferID];
			if( tr == null )
			{
				return;
			}

			Array.Copy(Data, 0, tr.AssetData, tr.Received, Data.Length);
			tr.Received += Data.Length;

			// If we've gotten all the data, mark it completed.
			if( tr.Received >= tr.Size )
			{
				tr.Completed = true;
			}
			
		}

        /// <summary>
        /// Confirms SL's receipt of a Xfer upload packet
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="simulator"></param>
        public void ConfirmXferPacketCallbackHandler(Packet packet, Simulator simulator)
		{
            ConfirmXferPacketPacket reply = (ConfirmXferPacketPacket)packet;

			ulong XferID   = reply.XferID.ID;
			uint PacketNum = reply.XferID.Packet;
		}


        /// <summary>
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="simulator"></param>
        public void RequestXferCallbackHandler(Packet packet, Simulator simulator)
		{
            RequestXferPacket reply = (RequestXferPacket)packet;

            ulong XferID   = reply.XferID.ID;
			LLUUID AssetID = reply.XferID.VFileID;

			TransferRequest tr = (TransferRequest)htUploadRequests[AssetID];

			byte[] packetData = new byte[1004];

            // Prefix the first Xfer packet with the data length
            // FIXME: Apply endianness patch
            Array.Copy(BitConverter.GetBytes((int)tr.AssetData.Length), 0, packetData, 0, 4);
			Array.Copy(tr.AssetData, 0, packetData, 4, 1000);

            packet = AssetPacketHelpers.SendXferPacket(XferID, packetData, 0);
			slClient.Network.SendPacket(packet);


			// TODO: This for loop should be removed and these uploads should take place in
			// a call back handler for ConfirmXferPacket, so that each packet is only sent
            // after confirming SL's receipt of the previous packet
			int numPackets = tr.AssetData.Length / 1000;
			for( uint i = 1; i<numPackets; i++ )
			{
				packetData = new byte[1000];
				Array.Copy(tr.AssetData, i*1000, packetData, 0, 1000);

				packet = AssetPacketHelpers.SendXferPacket(XferID, packetData, i);
				slClient.Network.SendPacket(packet);
			}

			int lastLen = tr.AssetData.Length - (numPackets * 1000);
			packetData = new byte[ lastLen ];
			Array.Copy(tr.AssetData, numPackets * 1000, packetData, 0, lastLen);

			uint lastPacket = (uint)int.MaxValue + (uint)numPackets + (uint)1;
			packet = AssetPacketHelpers.SendXferPacket(XferID, packetData, lastPacket);
			slClient.Network.SendPacket(packet);
		}
	}
}
