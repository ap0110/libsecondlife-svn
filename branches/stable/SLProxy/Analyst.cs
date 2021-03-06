/*
 * Analyst.cs: proxy that makes packet inspection and modifcation interactive
 *   See the README for usage instructions.
 *
 * Copyright (c) 2006 Austin Jennings
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

using SLProxy;
using libsecondlife;
using Nwc.XmlRpc;

using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

public class Analyst {
	private static SecondLife client;
	private static ProtocolManager protocolManager;
	private static Proxy proxy;
	private static Hashtable commandDelegates = new Hashtable();
	private static Hashtable loggedPackets = new Hashtable();
	private static string logGrep = null;
	private static Hashtable modifiedPackets = new Hashtable();
	private static LLUUID agentID;
	private static LLUUID sessionID;
	private static bool logLogin = false;

	public static void Main(string[] args) {
		// configure the proxy
		client = new SecondLife("../data/keywords.txt", "../data/message_template.msg");
		protocolManager = client.Protocol;
		ProxyConfig proxyConfig = new ProxyConfig("Analyst", "austin.jennings@gmail.com", protocolManager, args);
		proxy = new Proxy(proxyConfig);

		// build the table of /command delegates
		InitializeCommandDelegates();

		// add delegates for login
		proxy.SetLoginRequestDelegate(new XmlRpcRequestDelegate(LoginRequest));
		proxy.SetLoginResponseDelegate(new XmlRpcResponseDelegate(LoginResponse));

		// add a delegate for outgoing chat
		proxy.AddDelegate("ChatFromViewer", Direction.Incoming, new PacketDelegate(ChatFromViewerIn));
		proxy.AddDelegate("ChatFromViewer", Direction.Outgoing, new PacketDelegate(ChatFromViewerOut));

		//  handle command line arguments
		foreach (string arg in args)
			if (arg == "--log-all")
				LogAll();
			else if (arg == "--log-login")
				logLogin = true;

		// start the proxy
		proxy.Start();
	}

	// LoginRequest: dump a login request to the console
	private static void LoginRequest(XmlRpcRequest request) {
		if (logLogin) {
			Console.WriteLine("==> Login Request");
			Console.WriteLine(request);
		}
	}

	// Loginresponse: dump a login response to the console
	private static void LoginResponse(XmlRpcResponse response) {
		Hashtable values = (Hashtable)response.Value;
		if (values.Contains("agent_id"))
			agentID = new LLUUID((string)values["agent_id"]);
		if (values.Contains("session_id"))
			sessionID = new LLUUID((string)values["session_id"]);

		if (logLogin) {
			Console.WriteLine("<== Login Response");
			Console.WriteLine(response);
		}
	}

	// ChatFromViewerIn: incoming ChatFromViewer delegate; shouldn't be possible, but just in case...
	private static Packet ChatFromViewerIn(Packet packet, IPEndPoint sim) {
		if (loggedPackets.Contains("ChatFromViewer") || modifiedPackets.Contains("ChatFromViewer"))
			// user has asked to log or modify this packet
			return Analyze(packet, sim, Direction.Incoming);
		else
			// return the packet unmodified
			return packet;
	}

	// ChatFromViewerOut: outgoing ChatFromViewer delegate; check for Analyst commands
	private static Packet ChatFromViewerOut(Packet packet, IPEndPoint sim) {
		// deconstruct the packet
		Hashtable blocks = PacketUtility.Unbuild(packet);
		string message = DataConvert.toChoppedString(PacketUtility.GetField(blocks, "ChatData", "Message"));

		if (message.Length > 1 && message[0] == '/') {
			string[] words = message.Split(' ');
			if (commandDelegates.Contains(words[0])) {
				// this is an Analyst command; act on it and drop the chat packet
				((CommandDelegate)commandDelegates[words[0]])(words);
				return null;
			}
		}

		if (loggedPackets.Contains("ChatFromViewer") || modifiedPackets.Contains("ChatFromViewer"))
			// user has asked to log or modify this packet
			return Analyze(packet, sim, Direction.Outgoing);
		else
			// return the packet unmodified
			return packet;
	}

	// CommandDelegate: specifies a callback delegate for a /command
	private delegate void CommandDelegate(string[] words);

	// InitializeCommandDelegates: configure Analyst's commands
	private static void InitializeCommandDelegates() {
		commandDelegates["/log"] = new CommandDelegate(CmdLog);
		commandDelegates["/-log"] = new CommandDelegate(CmdNoLog);
		commandDelegates["/grep"] = new CommandDelegate(CmdGrep);
		commandDelegates["/set"] = new CommandDelegate(CmdSet);
		commandDelegates["/-set"] = new CommandDelegate(CmdNoSet);
		commandDelegates["/inject"] = new CommandDelegate(CmdInject);
		commandDelegates["/in"] = new CommandDelegate(CmdInject);
	}

	// CmdLog: handle a /log command
	private static void CmdLog(string[] words) {
		if (words.Length != 2)
			SayToUser("Usage: /log <packet name>");
		else if (words[1] == "*") {
			LogAll();
			SayToUser("logging all packets");
		} else {
			loggedPackets[words[1]] = null;
			if (words[1] != "ChatFromViewer") {
				proxy.AddDelegate(words[1], Direction.Incoming, new PacketDelegate(AnalyzeIn));
				proxy.AddDelegate(words[1], Direction.Outgoing, new PacketDelegate(AnalyzeOut));
			}
			SayToUser("logging " + words[1]);
		}
	}

	// CmdNoLog: handle a /-log command
	private static void CmdNoLog(string[] words) {
		if (words.Length != 2)
			SayToUser("Usage: /-log <packet name>");
		else if (words[1] == "*") {
			NoLogAll();
			SayToUser("stopped logging all packets");
		} else {
			loggedPackets.Remove(words[1]);

			if (!modifiedPackets.Contains(words[1])) {
				if (words[1] != "ChatFromViewer") {
					proxy.RemoveDelegate(words[1], Direction.Incoming);
					proxy.RemoveDelegate(words[1], Direction.Outgoing);
				}
			}

			SayToUser("stopped logging " + words[1]);
		}
	}

	// CmdGrep: handle a /grep command
	private static void CmdGrep(string[] words) {
		if (words.Length == 1) {
			logGrep = null;
			SayToUser("stopped filtering logs");
		} else {
			string[] regexArray = new string[words.Length - 1];
			Array.Copy(words, 1, regexArray, 0, words.Length - 1);
			logGrep = String.Join(" ", regexArray);
			SayToUser("filtering log with " + logGrep);
		}
	}

	// CmdSet: handle a /set command
	private static void CmdSet(string[] words) {
		if (words.Length < 5)
			SayToUser("Usage: /set <packet name> <block> <field> <value>");
		else {
			string[] valueArray = new string[words.Length - 4];
			Array.Copy(words, 4, valueArray, 0, words.Length - 4);
			string valueString = String.Join(" ", valueArray);
			object value;
			try {
				value = MagicCast(words[1], words[2], words[3], valueString);
			} catch (Exception e) {
				SayToUser(e.Message);
				return;
			}

			Hashtable fields;
			if (modifiedPackets.Contains(words[1]))
				fields = (Hashtable)modifiedPackets[words[1]];
			else
				fields = new Hashtable();

			fields[new BlockField(words[2], words[3])] = value;
			modifiedPackets[words[1]] = fields;

			if (words[1] != "ChatFromViewer") {
				proxy.AddDelegate(words[1], Direction.Incoming, new PacketDelegate(AnalyzeIn));
				proxy.AddDelegate(words[1], Direction.Outgoing, new PacketDelegate(AnalyzeOut));
			}

			SayToUser("setting " + words[1] + "." + words[2] + "." + words[3] + " = " + valueString);
		}
	}

	// CmdNoSet: handle a /-set command
	private static void CmdNoSet(string[] words) {
		if (words.Length == 2 && words[1] == "*") {
			foreach (string name in modifiedPackets.Keys)
				if (!loggedPackets.Contains(name) && name != "ChatFromViewer") {
					proxy.RemoveDelegate(name, Direction.Incoming);
					proxy.RemoveDelegate(name, Direction.Outgoing);
				}
			modifiedPackets = new Hashtable();

			SayToUser("stopped setting all fields");
		} else if (words.Length == 4) {
			if (modifiedPackets.Contains(words[1])) {
				Hashtable fields = (Hashtable)modifiedPackets[words[1]];
				fields.Remove(new BlockField(words[2], words[3]));

				if (fields.Count == 0) {
					modifiedPackets.Remove(words[1]);

					if (!loggedPackets.Contains(words[1])) {
						if (words[1] != "ChatFromViewer") {
							proxy.RemoveDelegate(words[1], Direction.Incoming);
							proxy.RemoveDelegate(words[1], Direction.Outgoing);
						}
					}
				}
			}

			SayToUser("stopped setting " + words[1] + "." + words[2] + "." + words[3]);
		} else
			SayToUser("Usage: /-set <packet name> <block> <field>");
	}

	// CmdInject: handle an /inject command
	private static void CmdInject(string[] words) {
		if (words.Length < 2)
			SayToUser("Usage: /inject <packet file> [value]");
		else {
			string[] valueArray = new string[words.Length - 2];
			Array.Copy(words, 2, valueArray, 0, words.Length - 2);
			string value = String.Join(" ", valueArray);

			FileStream fs = null;
			StreamReader sr = null;
			Direction direction = Direction.Incoming;
			string name = null;
			Hashtable blocks = new Hashtable();
			string block = null;
			Hashtable fields = new Hashtable();

			try {
				fs = File.OpenRead(words[1] + ".packet");
				sr = new StreamReader(fs);

				string line;
				while ((line = sr.ReadLine()) != null) {
					Match match;

					if (name == null) {
						match = (new Regex(@"^\s*(in|out)\s+(\w+)\s*$")).Match(line);
						if (!match.Success) {
							SayToUser("expecting direction and packet name, got: " + line);
							return;
						}

						string lineDir = match.Groups[1].Captures[0].ToString();
						string lineName = match.Groups[2].Captures[0].ToString();

						if (lineDir == "in")
							direction = Direction.Incoming;
						else if (lineDir == "out")
							direction = Direction.Outgoing;
						else {
							SayToUser("expecting 'in' or 'out', got: " + line);
							return;
						}

						name = lineName;
					} else {
						match = (new Regex(@"^\s*\[(\w+)\]\s*$")).Match(line);
						if (match.Success) {
							if (block != null)
								blocks[fields] = block;
							block = match.Groups[1].Captures[0].ToString();
							fields = new Hashtable();
							continue;
						}

						if (block == null) {
							SayToUser("expecting block name, got: " + line);
							return;
						}

						match = (new Regex(@"^\s*(\w+)\s*=\s*(.*)$")).Match(line);
						if (match.Success) {
							string lineField = match.Groups[1].Captures[0].ToString();
							string lineValue = match.Groups[2].Captures[0].ToString();

							if (lineValue == "$Value")
								fields[lineField] = MagicCast(name, block, lineField, value);
							else if (lineValue == "$UUID")
								fields[lineField] = new LLUUID(true);
							else if (lineValue == "$AgentID")
								fields[lineField] = agentID;
							else if (lineValue == "$SessionID")
								fields[lineField] = sessionID;
							else
								fields[lineField] = MagicCast(name, block, lineField, lineValue);

							continue;
						}

						SayToUser("expecting block name or field, got: " + line);
						return;
					}
				}

				if (name == null) {
					SayToUser("expecting direction and packet name, got EOF");
					return;
				}

				if (block != null)
					blocks[fields] = block;

				byte flags = Helpers.MSG_RELIABLE;
				if (protocolManager.Command(name).Encoded)
					flags |= Helpers.MSG_ZEROCODED;
				Packet packet = PacketBuilder.BuildPacket(name, protocolManager, blocks, flags);
				proxy.InjectPacket(packet, direction);

				SayToUser("injected " + words[1]);
			} catch (Exception e) {
				SayToUser("failed to inject " + words[1] + ": " + e.Message);
			} finally {
				if (fs != null)
					fs.Close();
				if (sr != null)
					sr.Close();
			}
		}
	}

	// SayToUser: send a message to the user as in-world chat
	private static void SayToUser(string message) {
		Hashtable blocks = new Hashtable();
		Hashtable fields;
		fields = new Hashtable();
		fields["FromName"] = "Analyst";
		fields["SourceID"] = new LLUUID(true);
		fields["OwnerID"] = agentID;
		fields["SourceType"] = (byte)2;
		fields["ChatType"] = (byte)1;
		fields["Audible"] = (byte)1;
		fields["Position"] = new LLVector3(0, 0, 0);
		fields["Message"] = message;
		blocks[fields] = "ChatData";
		Packet packet = PacketBuilder.BuildPacket("ChatFromSimulator", protocolManager, blocks, Helpers.MSG_RELIABLE);
		proxy.InjectPacket(packet, Direction.Incoming);
	}

	// BlockField: product type for a block name and field name
	private struct BlockField {
		public string block;
		public string field;

		public BlockField(string block, string field) {
			this.block = block;
			this.field = field;
		}
	}

	// MagicCast: given a packet/block/field name and a string, convert the string to a value of the appropriate type
	private static object MagicCast(string name, string block, string field, string value) {
		MapPacket packetMap;
		try {
			packetMap = protocolManager.Command(name);
		} catch {
			throw new Exception("unkown packet " + name);
		}

		foreach (MapBlock blockMap in packetMap.Blocks) {
			if (blockMap.Name != block)
				continue;

			foreach (MapField fieldMap in blockMap.Fields) {
				if (fieldMap.Name != field)
					continue;

				try {
					switch (fieldMap.Type) {
						case FieldType.U8:
							return Convert.ToByte(value);
						case FieldType.U16:
							return Convert.ToUInt16(value);
						case FieldType.U32:
							return Convert.ToUInt32(value);
						case FieldType.U64: // XXX: verify endianness
							ulong ulongVal = Convert.ToUInt64(value);
							return new U64((uint)((ulongVal >> 32) % 4294967296), (uint)(ulongVal % 4294967296));
						case FieldType.S8:
							return Convert.ToSByte(value);
						case FieldType.S16:
							return Convert.ToInt16(value);
						case FieldType.S32:
							return Convert.ToInt32(value);
						case FieldType.S64:
							return Convert.ToInt64(value);
						case FieldType.F32:
							return Convert.ToSingle(value);
						case FieldType.F64:
							return Convert.ToDouble(value);
						case FieldType.LLUUID:
							return new LLUUID(value);
						case FieldType.BOOL:
							if (value.ToLower() == "true")
								return true;
							else if (value.ToLower() == "false")
								return false;
							else
								throw new Exception();
						case FieldType.LLVector3:
							Match vector3Match = (new Regex(@"<\s*(-?[0-9.]+)\s*,\s*(-?[0-9.]+)\s*,\s*(-?[0-9.]+)\s*>")).Match(value);
							if (!vector3Match.Success)
								throw new Exception();
							return new LLVector3
								(Convert.ToSingle(vector3Match.Groups[1].Captures[0].ToString())
								,Convert.ToSingle(vector3Match.Groups[2].Captures[0].ToString())
								,Convert.ToSingle(vector3Match.Groups[3].Captures[0].ToString())
								);
						case FieldType.LLVector3d:
							Match vector3dMatch = (new Regex(@"<\s*(-?[0-9.]+)\s*,\s*(-?[0-9.]+)\s*,\s*(-?[0-9.]+)\s*>")).Match(value);
							if (!vector3dMatch.Success)
								throw new Exception();
							return new LLVector3d
								(Convert.ToDouble(vector3dMatch.Groups[1].Captures[0].ToString())
								,Convert.ToDouble(vector3dMatch.Groups[2].Captures[0].ToString())
								,Convert.ToDouble(vector3dMatch.Groups[3].Captures[0].ToString())
								);
						case FieldType.LLVector4:
							Match vector4Match = (new Regex(@"<\s*(-?[0-9.]+)\s*,\s*(-?[0-9.]+)\s*,\s*(-?[0-9.]+)\s*,\s*(-?[0-9.]+)\s*>")).Match(value);
							if (!vector4Match.Success)
								throw new Exception();
							float vector4X = Convert.ToSingle(vector4Match.Groups[1].Captures[0].ToString());
							float vector4Y = Convert.ToSingle(vector4Match.Groups[2].Captures[0].ToString());
							float vector4Z = Convert.ToSingle(vector4Match.Groups[3].Captures[0].ToString());
							float vector4S = Convert.ToSingle(vector4Match.Groups[4].Captures[0].ToString());
							byte[] vector4Bytes = new byte[16];
							Array.Copy(BitConverter.GetBytes(vector4X), 0, vector4Bytes,  0, 4);
							Array.Copy(BitConverter.GetBytes(vector4Y), 0, vector4Bytes,  4, 4);
							Array.Copy(BitConverter.GetBytes(vector4Z), 0, vector4Bytes,  8, 4);
							Array.Copy(BitConverter.GetBytes(vector4S), 0, vector4Bytes, 12, 4);
							return new LLVector4(vector4Bytes, 0);
						case FieldType.LLQuaternion:
							Match quaternionMatch = (new Regex(@"<\s*(-?[0-9.]+)\s*,\s*(-?[0-9.]+)\s*,\s*(-?[0-9.]+)\s*>")).Match(value);
							if (!quaternionMatch.Success)
								throw new Exception();
							float quaternionX = Convert.ToSingle(quaternionMatch.Groups[1].Captures[0].ToString());
							float quaternionY = Convert.ToSingle(quaternionMatch.Groups[2].Captures[0].ToString());
							float quaternionZ = Convert.ToSingle(quaternionMatch.Groups[3].Captures[0].ToString());
							byte[] quaternionBytes = new byte[12];
							Array.Copy(BitConverter.GetBytes(quaternionX), 0, quaternionBytes,  0, 4);
							Array.Copy(BitConverter.GetBytes(quaternionY), 0, quaternionBytes,  4, 4);
							Array.Copy(BitConverter.GetBytes(quaternionZ), 0, quaternionBytes,  8, 4);
							return new LLQuaternion(quaternionBytes, 0);
						case FieldType.IPADDR:
							return IPAddress.Parse(value);
						case FieldType.IPPORT:
							return Convert.ToUInt16(value);
						case FieldType.Variable:
							Match match = Regex.Match(value, @"^0x([0-9a-fA-F]{2})*", RegexOptions.IgnoreCase);
							if (match.Success) {
								byte[] buf = new byte[match.Groups[1].Captures.Count];
								int i = 0;
								foreach (Capture capture in match.Groups[1].Captures)
									buf[i++] = Byte.Parse(capture.ToString(), NumberStyles.AllowHexSpecifier);
								return buf;
							} else
								return value;
					}
				} catch {
					throw new Exception("unable to interpret " + value + " as " + fieldMap.Type);
				}

				throw new Exception("unsupported field type " + fieldMap.Type);
			}

			throw new Exception("unknown field " + name + "." + block + "." + field);
		}

		throw new Exception("unknown block " + name + "." + block);
	}

	// AnalyzeIn: analyze an incoming packet
	private static Packet AnalyzeIn(Packet packet, IPEndPoint endPoint) {
		return Analyze(packet, endPoint, Direction.Incoming);
	}

	// AnalyzeOut: analyze an outgoing packet
	private static Packet AnalyzeOut(Packet packet, IPEndPoint endPoint) {
		return Analyze(packet, endPoint, Direction.Outgoing);
	}

	// Analyze: modify and/or log a pocket
	private static Packet Analyze(Packet packet, IPEndPoint endPoint, Direction direction) {
		if (modifiedPackets.Contains(packet.Layout.Name))
			try {
				Hashtable changes = (Hashtable)modifiedPackets[packet.Layout.Name];
				Hashtable blocks = PacketUtility.Unbuild(packet);
				foreach (BlockField blockField in changes.Keys)
					PacketUtility.SetField(blocks, blockField.block, blockField.field, changes[blockField]);
				packet = PacketBuilder.BuildPacket(packet.Layout.Name, protocolManager, blocks, packet.Data[0]);
			} catch (Exception e) {
				Console.WriteLine("failed to modify " + packet.Layout.Name + ": " + e.Message);
				Console.WriteLine(e.StackTrace);
			}

		if (loggedPackets.Contains(packet.Layout.Name))
			LogPacket(packet, endPoint, direction);

		return packet;
	}

	// LogAll: register logging delegates for all packets
	private static void LogAll() {
		RegisterDelegates(proxy, protocolManager.LowMaps);
		RegisterDelegates(proxy, protocolManager.MediumMaps);
		RegisterDelegates(proxy, protocolManager.HighMaps);
	}

	// NoLogAll: unregister logging delegates for all packets
	private static void NoLogAll() {
		UnregisterDelegates(proxy, protocolManager.LowMaps);
		UnregisterDelegates(proxy, protocolManager.MediumMaps);
		UnregisterDelegates(proxy, protocolManager.HighMaps);
	}

	// RegisterDelegates: register delegates for each packet in an array of packet maps
	private static void RegisterDelegates(Proxy proxy, MapPacket[] maps) {
		foreach (MapPacket map in maps)
			if (map != null) {
				loggedPackets[map.Name] = null;

				if (map.Name != "ChatFromViewer") {
					proxy.AddDelegate(map.Name, Direction.Incoming, new PacketDelegate(AnalyzeIn));
					proxy.AddDelegate(map.Name, Direction.Outgoing, new PacketDelegate(AnalyzeOut));
				}
			}
	}

	// UnregisterDelegates: unregister delegates for each packet in an array of packet maps
	private static void UnregisterDelegates(Proxy proxy, MapPacket[] maps) {
		foreach (MapPacket map in maps)
			if (map != null) {
				loggedPackets.Remove(map.Name);

				if (map.Name != "ChatFromViewer") {
					proxy.RemoveDelegate(map.Name, Direction.Incoming);
					proxy.RemoveDelegate(map.Name, Direction.Outgoing);
				}
			}
	}

	// LogPacket: dump a packet to the console
	private static void LogPacket(Packet packet, IPEndPoint endPoint, Direction direction) {
		if (logGrep != null) {
			bool match = false;
			foreach (Block block in packet.Blocks())
				foreach (Field field in block.Fields) {
					string value;
					if (field.Layout.Type == FieldType.Variable)
						value = DataConvert.toChoppedString(field.Data);
					else
						value = field.Data.ToString();
					if (Regex.Match(packet.Layout.Name + "." + block.Layout.Name + "." + field.Layout.Name + " = " + value, logGrep, RegexOptions.IgnoreCase).Success) {
						match = true;
						break;
					}

					// try matching variable fields in 0x notation
					if (field.Layout.Type == FieldType.Variable) {
						StringWriter sw = new StringWriter();
						sw.Write("0x");
						foreach (byte b in (byte[])field.Data)
							sw.Write("{0:x2}", b);
						if (Regex.Match(packet.Layout.Name + "." + block.Layout.Name + "." + field.Layout.Name + " = " + sw, logGrep, RegexOptions.IgnoreCase).Success) {
							match = true;
							break;
						}
					}
				}
			if (!match)
				return;
		}

		Console.WriteLine("{0} {1,21} {2,5} {3}{4}{5}"
				 ,direction == Direction.Incoming ? "<--" : "-->"
				 ,endPoint
				 ,packet.Sequence
				 ,InterpretOptions(packet.Data[0])
				 ,Environment.NewLine
				 ,packet
				 );
	}

	// InterpretOptions: produce a string representing a packet's header options
	private static string InterpretOptions(byte options) {
		return "["
		     + ((options & Helpers.MSG_APPENDED_ACKS) != 0 ? "Ack" : "   ")
		     + " "
		     + ((options & Helpers.MSG_RESENT)        != 0 ? "Res" : "   ")
		     + " "
		     + ((options & Helpers.MSG_RELIABLE)      != 0 ? "Rel" : "   ")
		     + " "
		     + ((options & Helpers.MSG_ZEROCODED)     != 0 ? "Zer" : "   ")
		     + "]"
		     ;
	}
}
