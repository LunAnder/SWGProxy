﻿using SWGProxy.Packets;
using SWGProxy.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace SWGProxy.Packets
{
	public class SOEPacket
	{
		#region Packet Header/Footer variables
		private short SOEOpcode = 9;
		private short Sequence = 0;
		private short Multi = 0;
		private bool UseCompression = false;
		#endregion
		#region Private Variables
		private List<SWGPacket> childPackets = new List<SWGPacket>();
		#endregion

		#region Constructors
		public SOEPacket() { }
		public SOEPacket(byte[] data)
		{
			// Begin reading of SOE packet
			PacketReader reader = new PacketReader(data);
			SOEOpcode = reader.ReadShort();

			// Setup SWG packet buffer and begin to read
			byte[] swgPacketBuffer = DecryptData(reader.ReadByteArray(data.Length - 4)); // - 4 b/c of (header + footer)
			PacketReader swgReader = new PacketReader(swgPacketBuffer);
			Sequence = swgReader.ReadShort();
			Multi = swgReader.ReadShort();

			while (swgReader.CanSeek())
			{
				int length = swgReader.ReadByte();
				if (length == 0) break;

				SWGPacket packet = PacketLookup.Find(swgReader.ReadByteArray(length));
				childPackets.Add(packet);
			}

			this.ToArray();
		}
		#endregion

		#region Public Methods
		public byte[] ToArray()
		{
			// Create SOE Wrapper
			List<byte> soeBuffer = new List<byte>();
			soeBuffer.AddRange(BitConverter.GetBytes(SOEOpcode));

			// Create SWG packet header
			List<byte> swgPacketBuffer = new List<byte>();
			swgPacketBuffer.AddRange(BitConverter.GetBytes(Sequence));
			swgPacketBuffer.AddRange(BitConverter.GetBytes(Multi));

			// Add SWG Packets
			foreach (SWGPacket packet in childPackets)
			{
				swgPacketBuffer.Add((byte)packet.ToArray().Length);
				swgPacketBuffer.AddRange(packet.ToArray());
			}
			swgPacketBuffer.Add((byte)(UseCompression ? 1 : 0));

			// Encrypt SWG Packet Buffer and add to SOE Packet Buffer
			soeBuffer.AddRange(EncryptData(swgPacketBuffer.ToArray()));

			// Append CRC to footer
			soeBuffer.AddRange(CalculateCRC(soeBuffer.ToArray()));
			return soeBuffer.ToArray();
		}
		#endregion
		#region Private Methods
		private byte[] EncryptData(byte[] data)
		{
			List<byte> newData = new List<byte>();
			uint mask = BitConverter.ToUInt32(Program.session.crcSeed, 0);

			int blockCount = data.Length / 4;
			int remainingBytes = data.Length % 4;

			for (int i = 0; i < blockCount; i++)
			{
				byte[] temp = new byte[4];
				for (int b = 0; b < 4; b++)
				{
					temp[b] = data[(i * 4) + b];
				}
				uint a = BitConverter.ToUInt32(temp, 0) ^ mask;
				mask = a;
				newData.AddRange(BitConverter.GetBytes(a));
			}
			for (int i = 0; i < remainingBytes; i++ )
			{
				newData.Add((byte)(data[(4 * blockCount) + i] ^ BitConverter.GetBytes(mask)[0]));
			}

			return newData.ToArray();
		}
		private byte[] DecryptData(byte[] data)
		{
			List<byte> newData = new List<byte>();
			uint mask = BitConverter.ToUInt32(Program.session.crcSeed, 0);

			int blockCount = data.Length / 4;
			int remainingBytes = data.Length % 4;

			for (int i = 0; i < blockCount; i++)
			{
				byte[] temp = new byte[4];
				for (int b = 0; b < 4; b++)
				{
					temp[b] = data[(i * 4) + b];
				}
				uint a = BitConverter.ToUInt32(temp, 0) ^ mask;
				mask = BitConverter.ToUInt32(temp, 0);
				newData.AddRange(BitConverter.GetBytes(a));
			}
			for (int i = 0; i < remainingBytes; i++)
			{
				newData.Add((byte)(data[(4 * blockCount) + i] ^ BitConverter.GetBytes(mask)[0]));
			}

			return newData.ToArray();
		}
		private byte[] CalculateCRC(byte[] data)
		{
			return BitConverter.GetBytes((short)(MessageCRC.GenerateCrc(data.ToArray(), MessageCRC.ParseNetByteInt(Program.session.crcSeed)) << 16 >> 16)).Reverse().ToArray();
		}
		#endregion
	}
}