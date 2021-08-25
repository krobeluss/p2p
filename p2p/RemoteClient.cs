using NaCl;
using P2P.Internal;
using P2P.Packets;
using P2P.Packets.Structures;
using P2P.Packets.Structures.Common;
using P2P.Packets.Structures.Encryption;
using P2P.Packets.Structures.P2P;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace P2P
{
    internal class RemoteClient : IDisposable
    {
        private IPEndPoint externalAddress;
        private IPEndPoint internalAddress;

        private IPEndPoint correctAddress;
        private PrivateNetwork network;

        private UInt32 remoteID;
        private int helloCount = 0;

        private IPacketDissector commonDissector = new CommonDissector();
        private IPacketDissector encryptionDissector;

        private NonceUtils nonceUtils;

        public bool IsConnected
        {
            get
            {
                return correctAddress != null;
            }
        }

        private RemoteClient()
        {

        }

        public void Dispose()
        {
            network.helloTask.Elapsed -= OnHello;
            network.pingTask.Elapsed -= OnPing;
        }

        public void ProcessPacket(IPayloadablePacketData packet, IPEndPoint from)
        {
            byte[] packetData = Decrypt(packet);

            if (packetData != null)
            {
                correctAddress = from;

                CommonLayer commonPacket = (CommonLayer)this.commonDissector.Dissect(packetData);

                switch(commonPacket.Header)
                {
                    case CommonHeaderConstants.HELLO:
                        break; // Todo get flags
                    case CommonHeaderConstants.PING:
                        Ping pingPacket = (Ping)commonPacket;

                        Pong pongPacket = new Pong();
                        pongPacket.Value = pingPacket.Value;

                        Send(Encrypt(commonDissector.Assembly(pongPacket)));
                        break;
                }
            }
        }

        private void OnHello(object source, System.Timers.ElapsedEventArgs e)
        {
            if (this.IsConnected)
                network.helloTask.Elapsed -= OnHello;
            else
            {
                helloCount++;
                SendHello();
            }
                
        }

        private void OnPing(object source, System.Timers.ElapsedEventArgs e)
        {
            if (this.IsConnected)
                SendPing();
        }

        private void Send(byte[] data)
        {
            if (correctAddress == null)
            {
                network?.SendTo(data, externalAddress);
                network?.SendTo(data, internalAddress);
            }
            else
                network.SendTo(data, correctAddress);
        }

        internal void SendHello()
        {
            Hello helloPacket = new Hello(); // TODO hello флаги

            Send(Encrypt(commonDissector.Assembly(helloPacket)));
        }

        internal void SendPing()
        {
            Ping pingPacket = new Ping(); // TODO hello флаги

            Send(Encrypt(commonDissector.Assembly(pingPacket)));
        }

        private byte[] Encrypt(byte[] packetData)
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

        private byte[] Decrypt(IPayloadablePacketData packetData)
        {
            if (typeof(NaClDissector).IsInstanceOfType(this.encryptionDissector))
            {
                NaClPacket naClPacket = (NaClPacket)this.encryptionDissector.Dissect(packetData.Payload);

                if (this.nonceUtils.TrackNonce(naClPacket.Payload))
                    return naClPacket.Payload;
                    
            }

            return null;
        }

        internal class Builder
        {
            private RemoteClient client = new RemoteClient();

            private int nonceLength = 0;

            public Builder()
            {

            }

            public Builder AddID(UInt32 id)
            {
                client.remoteID = id;
                return this;
            }

            public Builder AddNacl(Curve25519XSalsa20Poly1305 nacl)
            {
                client.encryptionDissector = new NaClDissector(nacl);
                nonceLength = Curve25519XSalsa20Poly1305.NonceLength;
                return this;
            }

            public Builder AddNonceUtils(bool even, int nonceLifetime, int timestampOffset)
            {
                client.nonceUtils = new NonceUtils(even, nonceLength, nonceLifetime, timestampOffset);
                return this;
            }

            public Builder AddAddEndpoint(IPEndPoint externalAddress, IPEndPoint internalAddress)
            {
                client.externalAddress = externalAddress;
                client.internalAddress = internalAddress;
                return this;
            }

            public RemoteClient Build()
            {
                return client;
            }
        }
    }
}
