﻿using SWGProxy.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SWGProxy.Packets
{
	public class LoginClusterStatus : SWGPacket
	{
		short operandCount = 2;
		int opcode = 0x3436AEB6;
		List<GameServer> gameServers = new List<GameServer>();

		public LoginClusterStatus() { }

		public LoginClusterStatus(byte[] original)
		{
			// Initialize a reader for reading
			PacketReader reader = new PacketReader(original);

			// Read SWG header
			operandCount = reader.ReadShort();
			opcode = reader.ReadInt();

			// Read servers into gameserver list
			int serverCount = reader.ReadInt();
			for(int i = 0; i < serverCount; i++)
			{
				GameServer server = new GameServer();
				server.ID = reader.ReadInt();
				server.IPAddress = reader.ReadASCII();
				server.Port = reader.ReadShort();
				server.PingPort = reader.ReadShort();
				server.Population = reader.ReadInt();
				server.MaxCapacity = reader.ReadInt();
				server.MaxCharactersPerServer = reader.ReadInt();
				server.Distance = reader.ReadInt();
				server.Status = reader.ReadInt();
				server.Recommended = reader.ReadByte();
				gameServers.Add(server);
			}
		}

		public byte[] ToArray()
		{
			// Initialize a writer for writing
			PacketWriter writer = new PacketWriter();

			// Build packet
			writer.writeShort(operandCount);
			writer.writeInt(opcode);

			writer.writeInt(gameServers.Count);
			foreach(GameServer server in gameServers)
			{
				writer.writeInt(server.ID);
				writer.writeASCII(server.IPAddress);
				writer.writeShort(server.Port);
				writer.writeShort(server.PingPort);
				writer.writeInt(server.Population);
				writer.writeInt(server.MaxCapacity);
				writer.writeInt(server.MaxCharactersPerServer);
				writer.writeInt(server.Distance);
				writer.writeInt(server.Status);
				writer.WriteByte(server.Recommended);
			}

			// Return packet's byte array
			return writer.ToArray();
		}
	}

	public struct GameServer
	{
		public int ID;
		public string IPAddress;
		public short Port;
		public short PingPort;
		public int Population;
		public int MaxCapacity;
		public int MaxCharactersPerServer;
		public int Distance;
		public int Status;
		public byte Recommended;
	}
}
