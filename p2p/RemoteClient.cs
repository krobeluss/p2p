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

        private UInt32 peerId;
        private int helloCount = 0;

        private IPacketDissector commonDissector = new CommonDissector();
        private IPacketDissector encryptionDissector;

        private NonceUtils nonceUtils;

        private PeerConfig peerConfig;

        //
        // STATS
        //

        private long lastDataReceiveTime;
        private long lastPacketReveiveTime;
        private long bytesSended;
        private long bytesReceived;
        private int compressionRatio;
        private int speed;
        private int ping;

        private long speedSum;
        private long lastSpeedMove;

        private IList<int> pings;

        public bool IsConnected
        {
            get
            {
                return correctAddress != null;
            }
        }

        public long LastDataReceiveTime { get => lastDataReceiveTime; }
        public long LastPacketReveiveTime { get => lastPacketReveiveTime; }
        public long BytesSended { get => bytesSended; }
        public long BytesReceived { get => bytesReceived; }
        public int CompressionRatio { get => compressionRatio; }
        public int Speed { get => speed; }
        public int Ping { get => ping; }

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
            try
            {
                byte[] packetData = Decrypt(packet);

                lastPacketReveiveTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                bytesReceived += packet.Payload.Length;


                if (packetData != null)
                {
                    correctAddress = from;

                    CommonLayer commonPacket = (CommonLayer)this.commonDissector.Dissect(packetData);

                    switch (commonPacket.Header)
                    {
                        case CommonHeaderConstants.HELLO:
                            Hello hello = (Hello)commonPacket;

                            if (peerConfig == null)                                
                                peerConfig = new PeerConfig();

                            if (hello.RemoteHelloReceived)
                                network.helloTask.Elapsed -= OnHello;

                            if(!hello.AnswerHello)
                                SendHello(true);

                            break; // Todo get flags
                        case CommonHeaderConstants.PING:
                            Ping pingPacket = (Ping)commonPacket;

                            Pong pongPacket = new Pong();
                            pongPacket.Value = pingPacket.Value;

                            Send(Encrypt(commonDissector.Assembly(pongPacket)));
                            break;
                        case CommonHeaderConstants.DATA:
                            lastDataReceiveTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                            // TODO process data packet

                            break;

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(from);
                Console.WriteLine(ex.ToString());
            }
        }

        private void OnHello(object source, System.Timers.ElapsedEventArgs e)
        {
            helloCount++;
            SendHello(false);  
        }

        internal void initTimers()
        {
            network.helloTask.Elapsed += OnHello;
            network.pingTask.Elapsed += OnPing;
        }

        private void OnPing(object source, System.Timers.ElapsedEventArgs e)
        {
            if (this.IsConnected)
                SendPing();
        }

        private void Send(byte[] data)
        {
            bytesSended += data.Length;

            if (correctAddress == null)
            {
                network?.SendTo(data, externalAddress);
                network?.SendTo(data, internalAddress);
            }
            else
                network.SendTo(data, correctAddress);
        }

        internal void Send(CommonLayer commonPacket)
        {
            Send(Encrypt(commonDissector.Assembly(commonPacket)));
        }

        internal void SendHello(bool answer)
        {
            Hello helloPacket = new Hello(); // TODO hello флаги
            helloPacket.RemoteHelloReceived = peerConfig != null;
            helloPacket.AnswerHello = answer;

            Send(helloPacket);
        }

        internal void SendPing()
        {
            Ping pingPacket = new Ping(); // TODO hello флаги

            Send(pingPacket);
        }

        private byte[] Encrypt(byte[] packetData)
        {
            if(typeof(NaClDissector).IsInstanceOfType(this.encryptionDissector))
            {
                NaClPacket naClPacket = new NaClPacket();
                naClPacket.Nonce = nonceUtils.GetNextNonce();
                naClPacket.Payload = packetData;

                byte[] returnData = this.encryptionDissector.Assembly(naClPacket);
                this.encryptionDissector.Dissect(returnData);

                return returnData;
            }

            return packetData; // Крипта отключена или неподдерживаемый диссектор диссектор
        }

        private byte[] Decrypt(IPayloadablePacketData packetData)
        {
            if (typeof(NaClDissector).IsInstanceOfType(this.encryptionDissector))
            {
                NaClPacket naClPacket = (NaClPacket)this.encryptionDissector.Dissect(packetData.Payload);

                if (this.nonceUtils.TrackNonce(naClPacket.Nonce))
                    return naClPacket.Payload;
                    
            }

            return null;
        }

        internal class Builder
        {
            private RemoteClient client = new RemoteClient();

            private int nonceLength = 0;

            public Builder(PrivateNetwork network)
            {
                client.network = network;
            }

            public Builder AddID(UInt32 id)
            {
                client.peerId = id;
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
                client.initTimers();
                return client;
            }
        }
    }
}
