using P2P.Packets.Structures.Common;
using P2P.Packets.Structures.P2P;
using System;
using System.Collections.Generic;
using System.Text;

namespace P2P.Packets.Structures
{
    internal class CommonDissector : IPacketDissector
    {
        public byte[] Assembly(IPacketData packet)
        {
            if (typeof(CommonLayer).IsInstanceOfType(packet))
            {
                CommonLayer commonPacket = (CommonLayer)packet;
                byte[] subPacketData = commonPacket.Assembly();
                byte[] packetData = new byte[subPacketData.Length + 1];

                packetData[0] = commonPacket.Header;
                Array.Copy(subPacketData, 0, packetData, 1, subPacketData.Length);

                return packetData;
            }

            throw new ArgumentException();
        }

        public IPacketData Dissect(byte[] data)
        {
            byte[] subPacketData = new byte[data.Length - 1];
            Array.Copy(data, 1, subPacketData, 0, subPacketData.Length);
         
            switch (data[0])
            {
                case CommonHeaderConstants.HELLO:
                    return new Hello(subPacketData);
                case CommonHeaderConstants.PING:
                    return new Ping(subPacketData);
                case CommonHeaderConstants.PONG:
                    return new Pong(subPacketData);
                case CommonHeaderConstants.UDP_DATA:
                    return new UdpData(subPacketData);
            }

            return null;
        }
    }
}
