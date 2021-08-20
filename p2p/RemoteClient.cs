using NaCl;
using P2P.Internal;
using P2P.Packets;
using P2P.Packets.Structures.Encryption;
using P2P.Packets.Structures.P2P;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace P2P
{
    internal class RemoteClient
    {
        private IPEndPoint externalAddress;
        private IPEndPoint internalAddress;

        private IPEndPoint correctAddress;
        private PrivateNetwork network;

        private UInt32 remoteID;
        private int helloCount = 0;

        IPacketDissector commonDissector;
        IPacketDissector encryptionDissector;

        NonceUtils nonceUtils;

        public bool IsConnected
        {
            get
            {
                return correctAddress != null;
            }
        }

        public RemoteClient(IPEndPoint externalAddress, IPEndPoint internalAddress, UInt32 remoteID)
        {
            this.externalAddress = externalAddress;
            this.internalAddress = internalAddress;
            this.remoteID = remoteID;
        }

        public void ProcessPacket(IPayloadablePacketData packet, IPEndPoint from)
        {
            if (typeof(NaClDissector).IsInstanceOfType(this.encryptionDissector))
            {
                NaClPacket naClPacket = (NaClPacket)this.encryptionDissector.Dissect(packet.Payload);

                if(this.nonceUtils.TrackNonce(naClPacket.Payload))
                {
                    // Парсим дальше пейлоад 
                }
                else
                {
                    // Нонс был использован
                }
            }
        }

        internal void PrepareHello()
        {
            Hello helloPacket = new Hello(); // TODO hello флаги

            byte[] data = Encrypt( commonDissector.Assembly(helloPacket) );
            
            if (correctAddress == null)
            {
                network.SendTo(data, externalAddress);
                network.SendTo(data, internalAddress);
            }
            else
                network.SendTo(data, correctAddress);
        }

        internal byte[] Encrypt(byte[] packetData)
        {
            if(typeof(NaClDissector).IsInstanceOfType(this.encryptionDissector))
            {
                NaClPacket naClPacket = new NaClPacket();
                naClPacket.Nonce = nonceUtils.GetNextNonce();
                naClPacket.Payload = packetData;

                return this.encryptionDissector.Assembly(naClPacket);
            }

            return packetData; // Крипта отключена или неподдерживаемый диссектор диссектор
        }
    }
}
