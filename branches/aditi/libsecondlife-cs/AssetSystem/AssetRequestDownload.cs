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

//#define DEBUG_PACKETS

using System;
using System.Collections.Generic;

using libsecondlife;

using libsecondlife.InventorySystem;

using libsecondlife.Packets;
using System.Threading;

namespace libsecondlife.AssetSystem
{
    class AssetRequestDownload
    {
        public LLUUID TransactionID;

        public ManualResetEvent Completed = new ManualResetEvent(false);
        public bool Status;
        public string StatusMsg;

        public int Size;
        public int Received;
        public byte[] AssetData;
        public SortedList<int, byte[]> AssetDataReceived = new SortedList<int, byte[]>();

        private uint _LastPacketTime;
        public uint LastPacketTime { get { return _LastPacketTime; } }

        public uint SecondsSinceLastPacket { get { return Helpers.GetUnixTime() - _LastPacketTime; } }

        public AssetRequestDownload(LLUUID TransID)
        {
            
            TransactionID = TransID;
            UpdateLastPacketTime();
        }

        public void UpdateLastPacketTime()
        {
            _LastPacketTime = Helpers.GetUnixTime();
        }
    }
}
