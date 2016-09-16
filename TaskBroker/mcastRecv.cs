using System.Net.Sockets;
using System.Net;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;


namespace multiCastRecv 
{
    
	class CastRecv 
	{
        bool shutdwn = false;
        string mcastGroup;
        string port;
        public delegate void onNewMessageContainer(byte[] data);
        public event onNewMessageContainer onNewMessage;

        public CastRecv(string _mcastGroup, string _port) 
		{
            mcastGroup = _mcastGroup;
            port = _port;
            new Thread(() => DoWork()).Start();
		}

        public byte[] ConvertToByteArray(IList<ArraySegment<byte>> list)
        {
            var bytes = new byte[list.Sum(asb => asb.Count)];
            int pos = 0;
            foreach (var asb in list)
            {
                Buffer.BlockCopy(asb.Array, asb.Offset, bytes, pos, asb.Count);
                pos += asb.Count;
            }
            return bytes;
        }

        public void Stop()
        {
            shutdwn = true;
        }



        void DoWork()
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            IPEndPoint ipep = new IPEndPoint(IPAddress.Any, int.Parse(port));
            s.Bind(ipep);

            IPAddress ip = IPAddress.Parse(mcastGroup);

            s.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(ip, IPAddress.Any));

            while (!shutdwn)
            {
                var buffer= new byte[1000];
                Console.WriteLine("Waiting for data..");
                s.Receive(buffer);
                Console.WriteLine("New message..");
                if (onNewMessage!=null )
                {
                    onNewMessage(buffer);
                }
            }
        }
//CastRecv("224.5.6.7", "5000");
	}







}