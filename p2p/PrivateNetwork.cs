using NaCl;
using P2P.Internal;
using P2P.Packets;
using P2P.Packets.Structures.Address;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using Timer = System.Timers.Timer;

namespace P2P
{
    public class PrivateNetwork : IDisposable 
    {
        private UdpClient socket;
        private ConcurrentDictionary<UInt32, RemoteClient> clients = new ConcurrentDictionary<UInt32, RemoteClient>();
        private UInt32 myPeerID;
        private NaCLKeyPair keyPair;
        private IPacketDissector adderssDissector = new AddressDissector();

        internal Timer helloTask = new Timer();
        internal Timer pingTask = new Timer();

        private Thread readSocketThread;

        private IPEndPoint stunEndpoint;
        private IPEndPoint externalEndpoint;
        private IPEndPoint internalEndpoint;

        internal NetworkConfig config = new NetworkConfig();

        public IPEndPoint ExternalEndpoint { get => externalEndpoint; }
        public IPEndPoint InternalEndpoint { get => internalEndpoint; }

        public void Dispose()
        {
            readSocketThread.Interrupt();
            readSocketThread.Join();

            socket.Close();

            helloTask?.Dispose();
            pingTask?.Dispose();
        }

        public void Start()
        {
            if (readSocketThread.IsAlive)
                throw new InvalidOperationException("Already started");

            readSocketThread = new Thread(ReadSocketThread);
            readSocketThread.Start();

            helloTask.Interval = config.HelloInterval;
            pingTask.Interval = config.PingInterval;

            helloTask.Start();
            pingTask.Start();
        }

        private void ReadSocketThread()
        {

            while (readSocketThread.IsAlive)
            {
                IPEndPoint from = null;
                byte[] data = socket.Receive(ref from);

                if (data.Length == 0)
                    continue;

                if(from.Equals(this.stunEndpoint))
                {
                    externalEndpoint = STUNParser.ParseSTUNResponse(data);
                    this.stunEndpoint = null;
                }
                else if(this.myPeerID != 0)
                {
                    IDAdderss adderss = (IDAdderss)adderssDissector.Dissect(data);

                    RemoteClient client;

                    if(clients.TryGetValue(adderss.FromID, out client))
                        client.ProcessPacket(adderss, from);
                }
            }
        }

        internal void SendTo(byte[] data, IPEndPoint to)
        {
            IDAdderss adderssPacket = new IDAdderss();
            adderssPacket.FromID = myPeerID;
            adderssPacket.Payload = data;

            byte[] packet = adderssDissector.Assembly(adderssPacket);

            socket.Send(packet, packet.Length, to);
        }

        public void AddPeer(uint peerID, byte[] publicKey, IPEndPoint externalAddress, IPEndPoint internalAddress)
        {

            if (myPeerID == 0)
                throw new InvalidOperationException("myPeerID");

            RemoteClient.Builder builder = new RemoteClient.Builder();

            builder
                .AddID(peerID)
                .AddAddEndpoint(externalAddress, internalAddress);

            if(this.keyPair.privateKey != null)
            {
                builder.AddNacl(new Curve25519XSalsa20Poly1305(this.keyPair.privateKey, publicKey));
                builder.AddNonceUtils(myPeerID > peerID, 5000, 0);
            }

            if (clients.TryAdd(peerID, builder.Build()))
            {

            }
            else
                throw new ArgumentException("peerID is dublicate");

        }

        public void ReceiveExternalIP(IPEndPoint stunServer)
        {
            byte[] data = STUNParser.CreateStunRequest(new byte[12]);
            socket.Send(data, data.Length, stunServer);

            this.stunEndpoint = stunServer;
        }

        public void ReceiveInternalIP()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    this.internalEndpoint = new IPEndPoint(ip, ((IPEndPoint)this.socket.Client.LocalEndPoint).Port);
                    return;
                }
            }
        }

        public class Builder
        {
            PrivateNetwork network = new PrivateNetwork();
            IPEndPoint endPoint;

            NetworkConfig config = new NetworkConfig();

            public Builder()
            {

            }

            public Builder SetMaxHelloAttemts(int count)
            {
                config.MaxHelloAttemts = count;

                return this;
            }

            public Builder AddNaCl(byte[] privateKey, byte[] publicKey )
            {
                network.keyPair = new NaCLKeyPair();
                network.keyPair.publicKey = publicKey;
                network.keyPair.privateKey = privateKey;

                return this;
            }

            public Builder AddNaCl()
            {
                network.keyPair = new NaCLKeyPair();
                Curve25519XSalsa20Poly1305.KeyPair(out network.keyPair.privateKey, out network.keyPair.publicKey);

                return this;
            }

            public Builder AddID(uint id)
            {
                network.myPeerID = id;

                return this;
            }

            public Builder AddBindAddress(IPEndPoint endPoint)
            {
                this.endPoint = endPoint;

                return this;
            }

            public PrivateNetwork Build()
            {
                // Build Socket

                if (endPoint != null)
                    network.socket = new UdpClient(endPoint);
                else
                    network.socket = new UdpClient();

                const int SIO_UDP_CONNRESET = -1744830452;
                byte[] inValue = new byte[] { 0 };
                byte[] outValue = new byte[] { 0 };

                network.socket.Client.IOControl(SIO_UDP_CONNRESET, inValue, outValue);
                network.config = config;

                return network;
            }
        }

        private struct NaCLKeyPair
        {
            public byte[] privateKey;
            public byte[] publicKey;
        }
    }
}
