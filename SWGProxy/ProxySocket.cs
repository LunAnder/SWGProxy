﻿using SWGProxy.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace SWGProxy
{
	public class ProxySocket
	{
		public IPEndPoint destination;
		public IPEndPoint origin;

		private Socket udpSock;
		private byte[] buffer;

		public ProxySocket(int port)
		{
			//Setup the socket and message buffer
			udpSock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			udpSock.Bind(new IPEndPoint(IPAddress.Any, port));
			buffer = new byte[1024];

			//Start listening for a new message.
			EndPoint newClientEP = new IPEndPoint(IPAddress.Any, 0);
			udpSock.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref newClientEP, DoReceiveFrom, udpSock);
		}

		private void DoReceiveFrom(IAsyncResult iar)
		{
			try
			{
				//Get the received message.
				Socket recvSock = (Socket)iar.AsyncState;
				EndPoint clientEP = new IPEndPoint(IPAddress.Any, 0);

				int msgLen = recvSock.EndReceiveFrom(iar, ref clientEP);
				byte[] localMsg = new byte[msgLen];
				Array.Copy(buffer, localMsg, msgLen);

				//Start listening for a new message.
				EndPoint newClientEP = new IPEndPoint(IPAddress.Any, 0);
				udpSock.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref newClientEP, DoReceiveFrom, udpSock);

				// Handle the received message
				Console.WriteLine("Recieved {0} bytes from {1}:{2}", msgLen,  ((IPEndPoint)clientEP).Address, ((IPEndPoint)clientEP).Port);

				// Found our client
				if (origin == null || (origin.Port != ((IPEndPoint)clientEP).Port) && ((IPEndPoint)clientEP).Address.Equals(origin.Address))
				{
					if (origin != null) Console.WriteLine("Found new client, did the old one get disconnected?");
					else Console.WriteLine("Found new client");
					origin = (IPEndPoint)clientEP;
				}

				// Message is C -> S
				if(((IPEndPoint)clientEP).Address.Equals(origin.Address))
				{
					Console.WriteLine("Forwarding packet to server");
					udpSock.SendTo(localMsg, destination);
				}
				// Message is S -> C
				else 
				{
					Console.WriteLine("Forwarding packet to client");

					if (localMsg[1] == 0x02) // SOE_SESSION_REPLY
					{
						byte[] xorKey = new byte[4];
						byte[] zlibFlags = new byte[3];

						Array.Copy(localMsg, 6, xorKey, 0, 4);
						Program.session = new Session(xorKey, null);


					}
					else if (localMsg[1] == 0x09) // SOE_CHL_DATA_A
					{
						PacketStream packet = new PacketStream(localMsg.ToArray());
						packet.xorData();
						localMsg = packet.getFinalizedPacket();
					}

					udpSock.SendTo(localMsg, origin);
				}
			}
			catch (Exception ex) 
			{
				EndPoint newClientEP = new IPEndPoint(IPAddress.Any, 0);
				udpSock.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref newClientEP, DoReceiveFrom, udpSock);
			}
		}
	}
}