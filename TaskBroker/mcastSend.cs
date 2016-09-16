using System;
using System.Net;
using System.Net.Sockets;

namespace multiCastSend
{
	public class send
	{
        public send(byte[] data, string mcastGroup = "224.0.0.0", int port = 7, int ttl = 2, int rep = 2) 
		{
			IPAddress ip;
			try 
			{
				Console.WriteLine("MCAST Send on Group: {0} Port: {1} TTL: {2}",mcastGroup,port,ttl);
				ip=IPAddress.Parse(mcastGroup);
				
				Socket s=new Socket(AddressFamily.InterNetwork, 
								SocketType.Dgram, ProtocolType.Udp);
				
				s.SetSocketOption(SocketOptionLevel.IP, 
					SocketOptionName.AddMembership, new MulticastOption(ip));

				s.SetSocketOption(SocketOptionLevel.IP, 
					SocketOptionName.MulticastTimeToLive, ttl);
			
				IPEndPoint ipep=new IPEndPoint(IPAddress.Parse(mcastGroup),port);

                Console.WriteLine("Connecting to MCAST Group...");

				s.Connect(ipep);

                for(int x=0;x<rep;x++) {
					Console.WriteLine("Sending Multicast...");
                    s.Send(data, data.Length, SocketFlags.None  );
                }

				Console.WriteLine("Closing Connection...");
				s.Close();
			} 
			catch(System.Exception e) { Console.Error.WriteLine(e.Message); }
		}
	}
}
