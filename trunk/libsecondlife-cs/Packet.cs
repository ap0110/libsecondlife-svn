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
using System.Net;
using System.Collections;

namespace libsecondlife
{
	public struct Field
	{
		public object Data;
		public MapField Layout;

		public override string ToString()
		{
			string output = "";

			if (Layout.Type == FieldType.Variable || Layout.Type == FieldType.Fixed)
			{
				bool printable = true;
				byte[] byteArray = (byte[])Data;

				for (int i = 0; i < byteArray.Length; ++i)
				{
					// Check if there are any unprintable characters in the array
					if ((byteArray[i] < 0x20 || byteArray[i] > 0x7E) && byteArray[i] != 0x09
						&& byteArray[i] != 0x0D)
					{
						printable = false;
					}
				}

				if (printable)
				{
					output += System.Text.Encoding.ASCII.GetChars(byteArray, 0, byteArray.Length);
				}
				else
				{
					for (int i = 0; i < byteArray.Length; i += 16)
					{
						output += Layout.Name + ": ";

						for (int j = 0; j < 16; j++)
						{
							if ((i + j) < byteArray.Length)
							{
								output += String.Format("{0:X} ", byteArray[i + j]);
							}
							else
							{
								output += "   ";
							}
						}

						for (int j = 0; j < 16 && (i + j) < byteArray.Length; j++)
						{
							if (byteArray[i + j] >= 0x20 && byteArray[i + j] < 0x7E)
							{
								output += (char)byteArray[i + j];
							}
							else
							{
								output += ".";
							}
						}

						output += "\n";
					}
				}
			}
			else
			{
				output += Layout.Name + ": " + Data.ToString();
			}

			return output;
		}
	}

	public struct Block
	{
		public ArrayList Fields;
		public MapBlock Layout;
	}

	public class Packet
	{
		public byte[] Data;
		public MapPacket Layout;

		public ushort Sequence
		{
			get
			{
				// The sequence number is the third and fourth bytes of the packet, stored 
				// in network order
				return (ushort)(Data[2] * 256 + Data[3]);
			}

			set
			{
				Data[2] = (byte)(value / 256);
				Data[3] = (byte)(value % 256);
			}
		}

		private ProtocolManager Protocol;

		public Packet(string command, ProtocolManager protocol, int length)
		{
			Protocol = protocol;
			Data = new byte[length];
			Layout = protocol.Command(command);

			if (Layout == null)
			{
				Helpers.Log("Attempting to build a packet with invalid command \"" + command + "\"", 
					Helpers.LogLevel.Error);
			}

			switch (Layout.Frequency)
			{
				case PacketFrequency.Low:
					// Set the low frequency identifier bits
					byte[] lowHeader = {0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF};
					Array.Copy(lowHeader, 0, Data, 0, 6);

					// Store the packet ID in network order
					Data[6] = (byte)(Layout.ID / 256);
					Data[7] = (byte)(Layout.ID % 256);

					break;
				case PacketFrequency.Medium:
					// Set the medium frequency identifier bit
					byte[] mediumHeader = {0x00, 0x00, 0x00, 0x00, 0xFF};
					Array.Copy(mediumHeader, 0, Data, 0, 5);
					Data[5] = (byte)Layout.ID;

					break;
				case PacketFrequency.High:
					byte[] highHeader = {0x00, 0x00, 0x00, 0x00};
					Array.Copy(highHeader, 0, Data, 0, 4);
					Data[4] = (byte)Layout.ID;

					break;
			}
		}

		public Packet(byte[] data, int length, ProtocolManager protocol)
		{
			Protocol = protocol;
			ushort command;
			Data = new byte[length];

			if (length < 5)
			{
				Helpers.Log("Received a packet with less than 5 bytes", Helpers.LogLevel.Warning);
				
				// Create an empty MapPacket
				Layout = new MapPacket();
				Layout.Blocks = new ArrayList();

				return;
			}

			if (data[4] == 0xFF)
			{
				if ((byte)data[5] == 0xFF)
				{
					// Low frequency
					command = (ushort)(data[6] * 256 + data[7]);

					Layout = protocol.Command(command, PacketFrequency.Low);
				}
				else
				{
					// Medium frequency
					command = (ushort)data[5];
					Layout = protocol.Command(command, PacketFrequency.Medium);
				}
			}
			else
			{
				// High frequency
				command = (ushort)data[4];
				Layout = protocol.Command(command, PacketFrequency.High);
			}

			if (Layout == null)
			{
				Helpers.Log("Received a packet with an unknown command ID", Helpers.LogLevel.Warning);

				// Create an empty MapPacket
				Layout = new MapPacket();
				Layout.Blocks = new ArrayList();
			}

			// Copy the network byte array to this packet's byte array
			Array.Copy(data, 0, Data, 0, length);
		}

		public ArrayList Blocks()
		{
			Field field;
			Block block;
			byte blockCount;
			int fieldSize;

			// Get the starting position of the SL payload (different than the UDP payload)
			int pos = HeaderLength();

			// Initialize the block list we are returning
			ArrayList blocks = new ArrayList();

			foreach (MapBlock blockMap in Layout.Blocks)
			{
				if (blockMap.Count == -1)
				{
					// Variable count block
					if (pos < Data.Length)
					{
						blockCount = Data[pos];
						pos++;
					}
					else

					{
						Helpers.Log("getBlocks(): goto 1 reached", Helpers.LogLevel.Warning);
						goto Done;
					}
				}
				else
				{
					blockCount = (byte)blockMap.Count;
				}

				for (int i = 0; i < blockCount; ++i)
				{
					// Create a new block to push back on the list
					block = new Block();
					block.Layout = blockMap;
					block.Fields = new ArrayList();

					foreach (MapField fieldMap in blockMap.Fields)
					{
						if (fieldMap.Type == FieldType.Variable)
						{
							if (fieldMap.Count == 1)
							{
								// Field length described with one byte
								if (pos < Data.Length)
								{
									fieldSize = (ushort)Data[pos];
									pos++;
								}
								else
								{
									Helpers.Log("getBlocks(): goto 2 reached", Helpers.LogLevel.Warning);
									goto BlockDone;
								}
							}
							else // (fieldMap.Count == 2)
							{
								// Field length described with two bytes
								if (pos + 1 < Data.Length)
								{
									fieldSize = (ushort)(Data[pos] + Data[pos + 1] * 256);
									pos += 2;
								}
								else
								{
									Helpers.Log("getBlocks(): goto 3 reached", Helpers.LogLevel.Warning);
									goto BlockDone;
								}
							}

							if (fieldSize != 0)
							{
								if (pos + fieldSize <= Data.Length)
								{
									// Create a new field to add to the fields for this block
									field = new Field();
									field.Data = GetField(Data, pos, fieldMap.Type, fieldSize);
									field.Layout = fieldMap;

									block.Fields.Add(field);

									pos += fieldSize;
								}
								else
								{
									Helpers.Log("getBlocks(): goto 4 reached", Helpers.LogLevel.Warning);
									goto BlockDone;
								}
							}
						}
						else if (fieldMap.Type == FieldType.Fixed)
						{
							fieldSize = fieldMap.Count;

							if (pos + fieldSize <= Data.Length)
							{
								// Create a new field to add to the fields for this block
								field = new Field();
								field.Data = GetField(Data, pos, fieldMap.Type, fieldSize);
								field.Layout = fieldMap;

								block.Fields.Add(field);

								pos += fieldSize;
							}
							else
							{
								Helpers.Log("getBlocks(): goto 4 reached", Helpers.LogLevel.Warning);
								goto BlockDone;
							}
						}
						else
						{
							for (int j = 0; j < fieldMap.Count; ++j)
							{
								fieldSize = (int)Protocol.TypeSizes[fieldMap.Type];

								if (pos + fieldSize <= Data.Length)
								{
									// Create a new field to add to the fields for this block
									field = new Field();
									field.Data = GetField(Data, pos, fieldMap.Type, fieldSize);
									field.Layout = fieldMap;

									block.Fields.Add(field);

									pos += fieldSize;
								}
								else
								{
									Helpers.Log("getBlocks(): goto 5 reached", Helpers.LogLevel.Warning);
									goto BlockDone;
								}
							}
						}
					}

				BlockDone:
					blocks.Add(block);
				}
			}

			Done:
				return blocks;
		}

		public object Field(string name)
		{
			foreach (Block block in Blocks())
			{
				foreach (Field field in block.Fields)
				{
					if (field.Layout.Name == name)
					{
						return field.Data;
					}
				}
			}

			return null;
		}

		public override string ToString()
		{
			string output = "";
			ArrayList blocks = Blocks();
			
			output += "---- " + Layout.Name + " ----\n";

			foreach (Block block in blocks)
			{
				output += "-- " + block.Layout.Name + " --\n";

				foreach (Field field in block.Fields)
				{
					output += field.ToString() + "\n";
				}
			}

			return output;
		}

		private object GetField(byte[] byteArray, int pos, FieldType type, int fieldSize)
		{
			switch (type)
			{
				case FieldType.U8:
					return byteArray[pos];
				case FieldType.U16:
				case FieldType.IPPORT:
					return (ushort)(byteArray[pos] + (byteArray[pos + 1] << 8));
				case FieldType.U32:
					return (uint)(byteArray[pos] + (byteArray[pos + 1] << 8) +
						(byteArray[pos + 2] << 16) + (byteArray[pos + 3] << 24));
				case FieldType.U64:
					return new U64(byteArray, pos);
				case FieldType.S8:
					return (sbyte)byteArray[pos];
				case FieldType.S16:
					return (short)(byteArray[pos] + (byteArray[pos + 1] << 8));
				case FieldType.S32:
					return byteArray[pos] + (byteArray[pos + 1] << 8) +
						(byteArray[pos + 2] << 16) + (byteArray[pos + 3] << 24);
				case FieldType.S64:
					return (long)(byteArray[pos] + (byteArray[pos + 1] << 8) +
						(byteArray[pos + 2] << 16) + (byteArray[pos + 3] << 24) +
						(byteArray[pos + 4] << 32) + (byteArray[pos + 5] << 40) +
						(byteArray[pos + 6] << 48) + (byteArray[pos + 7] << 56));
				case FieldType.F32:
					return BitConverter.ToSingle(byteArray, pos); // FIXME
				case FieldType.F64:
					return BitConverter.ToDouble(byteArray, pos); // FIXME
				case FieldType.LLUUID:
					return new LLUUID(byteArray, pos);
				case FieldType.BOOL:
					return (byteArray[pos] != 0) ? (bool)true : (bool)false;
				case FieldType.LLVector3:
					return new LLVector3(byteArray, pos);
				case FieldType.LLVector3d:
					return new LLVector3d(byteArray, pos);
				case FieldType.LLVector4:
					return new LLVector4(byteArray, pos);
				case FieldType.LLQuaternion:
					return new LLQuaternion(byteArray, pos);
				case FieldType.IPADDR:
					uint address = (uint)(byteArray[pos] + (byteArray[pos + 1] << 8) +
						(byteArray[pos + 2] << 16) + (byteArray[pos + 3] << 24));
					return new IPAddress(address);
				case FieldType.Variable:
				case FieldType.Fixed:
					byte[] bytes = new byte[fieldSize];
					Array.Copy(byteArray, pos, bytes, 0, fieldSize);
					return bytes;
			}

			return null;
		}

		public int HeaderLength()
		{
			switch (Layout.Frequency)
			{
				case PacketFrequency.High:
					return 5;
				case PacketFrequency.Medium:
					return 6;
				case PacketFrequency.Low:
					return 8;
			}

			return 0;
		}
	}

	public class PacketBuilder
	{
		public static Packet BuildPacket(string name, ProtocolManager protocol, Hashtable blocks)
		{
			Hashtable fields;
			ArrayList payload = new ArrayList();
			byte[] byteArray = new byte[1024];
			int length = 0;
			int blockCount = 0;
			int fieldLength = 0;
			IDictionaryEnumerator blocksEnum;

			MapPacket packetMap = protocol.Command(name);

			// Build the header
			#region Header
			switch (packetMap.Frequency)
			{
				case PacketFrequency.High:
					byteArray[4] = (byte)packetMap.ID;
					length = 5;
					break;
				case PacketFrequency.Medium:
					byteArray[4] = 0xFF;
					byteArray[5] = (byte)packetMap.ID;
					length = 6;
					break;
				case PacketFrequency.Low:
					byteArray[4] = 0xFF;
					byteArray[5] = 0xFF;
					byteArray[6] = (byte)(packetMap.ID / 256);
					byteArray[7] = (byte)(packetMap.ID % 256);
					length = 8;
					break;
			}
			#endregion Header

			foreach (MapBlock blockMap in packetMap.Blocks)
			{
				// If this is a variable count block, count the number of appearances of this block in the 
				// passed in Hashtable and prepend a counter byte
				#region VariableSize
				if (blockMap.Count == -1)
				{
					blockCount = 0;

					// Count the number of this type of block in the blocks Hashtable
					blocksEnum = blocks.GetEnumerator();

					while (blocksEnum.MoveNext())
					{
						if ((string)blocksEnum.Value == blockMap.Name)
						{
							blockCount++;
						}
					}

					if (blockCount > 255)
					{
						Helpers.Log("Trying to put more than 255 blocks in a variable block position, " +
							"this will not end well", Helpers.LogLevel.Error);
					}

					// Prepend the blocks with a count
					byteArray[length] = (byte)blockCount;
					length++;
				}
				#endregion VariablSize

				// Reset blockCount
				blockCount = 0;

				// Check for blocks of this type in the Hashtable
				#region BuildBlock
				blocksEnum = blocks.GetEnumerator();

				while (blocksEnum.MoveNext())
				{
					if ((string)blocksEnum.Value == blockMap.Name)
					{
						// Found a match of this block
						if (blockMap.Count == -1 || blockCount < blockMap.Count)
						{
							blockCount++;

							#region TryBlockTypecast
							try
							{
								fields = (Hashtable)blocksEnum.Key;
							}
							catch (Exception)
							{
								Helpers.Log("Something other than a field Hashtable was passed to BuildPacket " +
									"inside of the block Hashtable", Helpers.LogLevel.Warning);
								continue;
							}
							#endregion TryBlockTypecast

							foreach (MapField fieldMap in blockMap.Fields)
							{
								if (fields.ContainsKey(fieldMap.Name))
								{
									object field = fields[fieldMap.Name];

									#region AddField
									try
									{
										switch (fieldMap.Type)
										{
											case FieldType.U8:
												byteArray[length++] = (byte)field;
												break;
											case FieldType.U16:
											case FieldType.IPPORT:
												ushort fieldUShort = (ushort)field;
												byteArray[length++] = (byte)(fieldUShort % 256);
												fieldUShort >>= 8;
												byteArray[length++] = (byte)(fieldUShort % 256);
												break;
											case FieldType.U32:
												uint fieldUInt = (uint)field;
												byteArray[length++] = (byte)(fieldUInt % 256);
												fieldUInt >>= 8;
												byteArray[length++] = (byte)(fieldUInt % 256);
												fieldUInt >>= 8;
												byteArray[length++] = (byte)(fieldUInt % 256);
												fieldUInt >>= 8;
												byteArray[length++] = (byte)(fieldUInt % 256);
												break;
											case FieldType.U64:
												// FIXME: Apply endianness patch
												Array.Copy(((U64)field).GetBytes(), 0, byteArray, length, 8);
												length += 8;
												break;
											case FieldType.S8:
												byteArray[length++] = (byte)field;
												break;
											case FieldType.S16:
												// FIXME: Apply endianness patch
												Array.Copy(BitConverter.GetBytes((short)field), 0, byteArray, length, 2);
												length += 2;
												break;
											case FieldType.S32:
												// FIXME: Apply endianness patch
												Array.Copy(BitConverter.GetBytes((int)field), 0, byteArray, length, 4);
												length += 4;
												break;
											case FieldType.S64:
												// FIXME: Apply endianness patch
												Array.Copy(BitConverter.GetBytes((long)field), 0, byteArray, length, 8);
												length += 8;
												break;
											case FieldType.F32:
												Array.Copy(BitConverter.GetBytes((float)field), 0, byteArray, length, 4);
												length += 4;
												break;
											case FieldType.F64:
												Array.Copy(BitConverter.GetBytes((double)field), 0, byteArray, length, 8);
												length += 8;
												break;
											case FieldType.LLUUID:
												Array.Copy(((LLUUID)field).Data, 0, byteArray, length, 16);
												length += 16;
												break;
											case FieldType.BOOL:
												byteArray[length] = (byte)field;
												length++;
												break;
											case FieldType.LLVector3:
												Array.Copy(((LLVector3)field).GetBytes(), 0, byteArray, length, 12);
												length += 12;
												break;
											case FieldType.LLVector3d:
												Array.Copy(((LLVector3d)field).GetBytes(), 0, byteArray, length, 24);
												length += 24;
												break;
											case FieldType.LLVector4:
												Array.Copy(((LLVector4)field).GetBytes(), 0, byteArray, length, 16);
												length += 16;
												break;
											case FieldType.LLQuaternion:
												Array.Copy(((LLQuaternion)field).GetBytes(), 0, byteArray, length, 16);
												length += 16;
												break;
											case FieldType.IPADDR:
												Array.Copy(BitConverter.GetBytes((ushort)field), 0, byteArray, length, 2);
												length += 2;
												break;
											case FieldType.Variable:
												if (field.GetType().IsArray)
												{
													// Assume this is a byte array
													fieldLength = ((byte[])field).Length;

													if (fieldLength > 255)
													{
														Helpers.Log("Truncating variable (byte) field to 255 " +
															"characters", Helpers.LogLevel.Warning);

														fieldLength = 255;
													}
												}
												else
												{
													// Assume this is a string, add 1 for the null terminator
													fieldLength = ((string)field).Length + 1;

													if (fieldLength > 255)
													{
														Helpers.Log("Truncating variable (string) field to 255 " +
															"characters", Helpers.LogLevel.Warning);

														fieldLength = 255;
													}
												}

												if (fieldMap.Count == 2)
												{
													Array.Copy(BitConverter.GetBytes(fieldLength), 0, byteArray, 
														length, 2);
													length += 2;
												}
												else
												{
													if (fieldMap.Count != 1)
													{
														Helpers.Log("Variable field " + fieldMap.Name + " has a count of " + 
															+ fieldMap.Count + ", ignoring and assuming 1", 
															Helpers.LogLevel.Warning);
													}

													byteArray[length] = (byte)(fieldLength);
													length++;
												}
												
												if (field.GetType().IsArray)
												{
													// Assume this is a byte array
													Array.Copy((byte[])field, 0, byteArray, length, fieldLength);
												}
												else
												{
													// Assume this is a string, add 1 for the null terminator
													byte[] stringBytes = System.Text.Encoding.UTF8.GetBytes((string)field);
													Array.Copy(stringBytes, 0, byteArray, length, stringBytes.Length);
													fieldLength = stringBytes.Length + 1;
												}

												length += fieldLength;

												break;
											case FieldType.Fixed:
												Array.Copy((byte[])field, 0, byteArray, length, fieldMap.Count);
												length += fieldMap.Count;
												break;
											default:
												Helpers.Log("Unhandled field type " + fieldMap.Type + " during " +
													"packet construction", Helpers.LogLevel.Error);
												break;
										}
									}
									catch (Exception)
									{
										Helpers.Log("Data type " + field.GetType().ToString() + " for field " +
											fieldMap.Name + " doesn't match expected type " + fieldMap.Type.ToString(), 
											Helpers.LogLevel.Error);
										// This will fail for fixed or variable type packets, but it's a 
										// last ditch effort
										length += (int)protocol.TypeSizes[fieldMap.Type];
									}
									#endregion AddField
								}
								else
								{
									// This field wasn't passed in, create an empty version
									#region EmptyField
									if (fieldMap.Type == FieldType.Variable)
									{
										// Just set the counter to zero and move on
										if (fieldMap.Count == 2)
										{
											length += 2;
										}
										else
										{
											if (fieldMap.Count != 1)
											{
												Helpers.Log("Variable field " + fieldMap.Name + " has a count of " + 
													+ fieldMap.Count + ", ignoring and assuming 1", 
													Helpers.LogLevel.Warning);
											}

											length++;
										}
									}
									else if (fieldMap.Type == FieldType.Fixed)
									{
										length += fieldMap.Count;
									}
									else
									{
										length += (int)protocol.TypeSizes[fieldMap.Type];
									}
									#endregion EmptyField
								}
							}
						}
						else
						{
							Helpers.Log("Trying to build a " + packetMap.Name + " packet with too many " + 
								blockMap.Name + " blocks", Helpers.LogLevel.Warning);
						}
					}
				}
				#endregion BuildBlock

				// If this is a fixed count block and it doesn't appear in the Hashtable passed in, create 
				// empty filler blocks
				#region EmptyBlock
				if (blockCount == 0 && blockMap.Count != -1)
				{
					for (int i = 0; i < blockMap.Count; ++i)
					{
						foreach (MapField field in blockMap.Fields)
						{
							if (field.Type == FieldType.Variable)
							{
								length++;
							}
							else
							{
								length += (int)protocol.TypeSizes[field.Type];
							}
						}
					}
				}
				#endregion EmptyBlock
			}

			return new Packet(byteArray, length, protocol);
		}

		public static Packet PacketAck(ProtocolManager protocol, ArrayList acks)
		{
			Hashtable blocks = new Hashtable();
			Hashtable fields;

			foreach (uint ack in acks)
			{
				fields = new Hashtable();
				fields["ID"] = ack;
				blocks[fields] = "Packets";
			}

			return BuildPacket("PacketAck", protocol, blocks);
		}

		public static Packet CompleteAgentMovement(ProtocolManager protocol, LLUUID agentID, LLUUID sessionID,
			uint circuitCode)
		{
			Hashtable blocks = new Hashtable();
			Hashtable fields = new Hashtable();
			fields["AgentID"] = agentID;
			fields["SessionID"] = sessionID;
			fields["CircuitCode"] = circuitCode;
			blocks[fields] = "AgentData";

			return BuildPacket("CompleteAgentMovement", protocol, blocks);
		}

		public static Packet UseCircuitCode(ProtocolManager protocol, LLUUID agentID, LLUUID sessionID,
			uint circuitCode)
		{
			Hashtable blocks = new Hashtable();
			Hashtable fields = new Hashtable();
			fields["ID"] = agentID;
			fields["SessionID"] = sessionID;
			fields["Code"] = circuitCode;
			blocks[fields] = "CircuitCode";

			return BuildPacket("UseCircuitCode", protocol, blocks);
		}

		public static Packet CompletePingCheck(ProtocolManager protocol, byte pingID)
		{
			Hashtable blocks = new Hashtable();
			Hashtable fields = new Hashtable();
			fields["PingID"] = pingID;
			blocks[fields] = "PingID";

			return BuildPacket("CompletePingCheck", protocol, blocks);
		}

		public static Packet LogoutRequest(ProtocolManager protocol, LLUUID agentID, LLUUID sessionID)
		{
			Hashtable blocks = new Hashtable();
			Hashtable fields = new Hashtable();
			fields["AgentID"] = agentID;
			fields["SessionID"] = sessionID;
			blocks[fields] = "AgentData";

			return BuildPacket("LogoutRequest", protocol, blocks);
		}

		public static Packet DirLandQuery(ProtocolManager protocol, bool reservedNewbie, bool forSale, LLUUID queryID, 
			bool auction, uint queryFlags, LLUUID agentID, LLUUID sessionID)
		{
			Hashtable blocks = new Hashtable();
			Hashtable fields = new Hashtable();

			fields["ReservedNewbie"] = reservedNewbie;
			fields["ForSale"] = forSale;
			fields["QueryID"] = queryID;
			fields["Auction"] = auction;
			fields["QueryFlags"] = queryFlags;
			blocks[fields] = "QueryData";

			fields = new Hashtable();
			fields["AgentID"] = agentID;
			fields["SessionID"] = sessionID;
			blocks[fields] = "AgentData";

			return BuildPacket("DirLandQuery", protocol, blocks);
		}

		public static Packet InstantMessage(ProtocolManager protocol, LLUUID targetAgentID, LLUUID myAgentID, 
			uint parentEstateID, LLUUID regionID, LLVector3 position, byte offline, byte dialog, LLUUID id, 
			uint timestamp, string myAgentName, string message, string binaryBucket) 
		{
			Hashtable blocks = new Hashtable();
			Hashtable fields = new Hashtable();

			fields["FromAgentID"] = myAgentID;
			fields["ToAgentID"] = targetAgentID;
			fields["ParentEstateID"] = parentEstateID;
			fields["RegionID"] = regionID;
			fields["Position"] = position;
			fields["Offline"] = offline;
			fields["Dialog"] = dialog;
			fields["ID"] = id;
			fields["Timestamp"] = timestamp;
			fields["FromAgentName"] = myAgentName;
			fields["Message"] = message;
			fields["BinaryBucket"] = binaryBucket;
			blocks[fields] = "MessageBlock";

			return BuildPacket("ImprovedInstantMessage", protocol, blocks);
		}

		public static Packet Chat(ProtocolManager protocol, LLUUID myAgentID, LLUUID mySessionID, string message,
			byte type, int channel, byte command, LLUUID commandID, float radius, LLVector3 position) 
		{
			Hashtable blocks = new Hashtable();
			Hashtable agentData = new Hashtable();
			Hashtable chatData = new Hashtable();
			Hashtable conversationData = new Hashtable();

			// Agent Data Block
			agentData["AgentID"] = myAgentID;
			agentData["SessionID"] = mySessionID;
			blocks[agentData] = "AgentData";

			// Chat Data Block
			chatData["Message"] = message;
			chatData["Type"] = type;
			chatData["Channel"] = channel;
			blocks[chatData] = "ChatData";

			// Conversation Data Block
			conversationData["Command"] = command;
			conversationData["CommandID"] = commandID;
			conversationData["Radius"] = radius;
			conversationData["Position"] = position;
			blocks[conversationData] = "ConversationData";

			return BuildPacket("ChatFromViewer", protocol, blocks);
		}

//		public static Packet ObjectAddSimple(ProtocolManager protocol, PrimObject objectData, LLUUID senderID, 
//			LLVector3 position, LLVector3 rayStart)
//		{
//			LLUUID woodTexture = new LLUUID("8955674724cb43ed920b47caed15465f");
//			LLUUID rayTargetID = new LLUUID("0f5d10f1f0a38634e893b70e00000000");
//			int length = 6 + 60 + 2 + objectData.NameValue.Length + 1 + 36 + 2 + 40 + 29;
//			Packet packet = new Packet("ObjectAdd", protocol, length);
//			int pos = 6;
//
//			// InventoryData appears 0 times
//			packet.Data[pos] = 0;
//			pos++;
//
//			// InventoryFile.Filename is of 1 length
//			packet.Data[pos] = 1;
//			pos++;
//
//			// InventoryFile.Filename is just a null terminator
//			packet.Data[pos] = 0;
//			pos++;
//
//			// U32 AddFlags = 2
//			uint addFlags = 2;
//			Array.Copy(BitConverter.GetBytes(addFlags), 0, packet.Data, pos, 4);
//			pos += 4;
//
//			packet.Data[pos] = (byte)objectData.PathTwistBegin;
//			pos++;
//
//			packet.Data[pos] = objectData.PathEnd;
//			pos++;
//
//			packet.Data[pos] = objectData.ProfileBegin;
//			pos++;
//
//			packet.Data[pos] = (byte)objectData.PathRadiusOffset;
//			pos++;
//
//			packet.Data[pos] = (byte)objectData.PathSkew;
//			pos++;
//
//			// SenderID
//			Array.Copy(senderID.Data, 0, packet.Data, pos, 16);
//			pos += 16;
//
//			// RayStart
//			Array.Copy(rayStart.GetBytes(), 0, packet.Data, pos, 12);
//			pos += 12;
//
//			packet.Data[pos] = objectData.ProfileCurve;
//			pos++;
//
//			packet.Data[pos] = objectData.PathScaleX;
//			pos++;
//
//			packet.Data[pos] = objectData.PathScaleY;
//			pos++;
//
//			// Set GroupID to zero
//			pos += 16;
//
//			packet.Data[pos] = objectData.Material;
//			pos++;
//
//			if (objectData.NameValue.Length != 0)
//			{
//				// NameValue, begins with two bytes describing the size
//				Array.Copy(BitConverter.GetBytes((ushort)(objectData.NameValue.Length + 1)), 0, packet.Data, pos, 2);
//				pos += 2;
//				System.Text.Encoding.UTF8.GetBytes(objectData.NameValue, 0, objectData.NameValue.Length, packet.Data, pos);
//				// Jump an extra spot for the null terminator
//				pos += objectData.NameValue.Length + 1;
//			}
//			else
//			{
//				// Set the two size bytes to zero and increment
//				pos += 2;
//			}
//
//			packet.Data[pos] = objectData.PathShearX;
//			pos++;
//
//			packet.Data[pos] = objectData.PathShearY;
//			pos++;
//
//			packet.Data[pos] = (byte)objectData.PathTaperX;
//			pos++;
//
//			packet.Data[pos] = (byte)objectData.PathTaperY;
//			pos++;
//
//			// RayEndIsIntersection
//			packet.Data[pos] = 0;
//			pos++;
//
//			// RayEnd is the position to place the object
//			Array.Copy(position.GetBytes(), 0, packet.Data, pos, 12);
//			pos += 12;
//
//			packet.Data[pos] = objectData.ProfileEnd;
//			pos++;
//
//			packet.Data[pos] = objectData.PathBegin;
//			pos++;
//
//			// BypassRaycast is 0
//			packet.Data[pos] = 0;
//			pos++;
//
//			// PCode? set to 9
//			packet.Data[pos] = 9;
//			pos++;
//
//			packet.Data[pos] = objectData.PathCurve;
//			pos++;
//
//			// Scale
//			Array.Copy(objectData.Scale.GetBytes(), 0, packet.Data, pos, 12);
//			pos += 12;
//
//			// State? is 0
//			packet.Data[pos] = 0;
//			pos++;
//
//			packet.Data[pos] = (byte)objectData.PathTwist;
//			pos++;
//
//			// TextureEntry, starts with two bytes describing the size
//			Array.Copy(BitConverter.GetBytes((ushort)40), 0, packet.Data, pos, 2);
//			pos += 2;
//			Array.Copy(woodTexture.Data, 0, packet.Data, pos, 16);
//			pos += 16;
//			// Fill in the rest of TextureEntry
//			pos += 19;
//			packet.Data[pos] = 0xe0;
//			pos += 5;
//
//			packet.Data[pos] = objectData.ProfileHollow;
//			pos++;
//
//			packet.Data[pos] = objectData.PathRevolutions;
//			pos++;
//
//			// Rotation
//			Array.Copy(objectData.Rotation.GetBytes(), 0, packet.Data, pos, 16);
//			pos += 16;
//
//			// RayTargetID
//			Array.Copy(rayTargetID.Data, 0, packet.Data, pos, 12);
//			pos += 12;
//			
//			// Set the packet flags
//			packet.Data[0] = /*Helpers.MSG_ZEROCODED +*/ Helpers.MSG_RELIABLE;
//
//			return packet;
//		}
	}
}