using NaCl;
using P2P.Internal;
using P2P.Packets;
using P2P.Packets.Structures.Address;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace P2P
{
    public class PrivateNetwork : IDisposable 
    {
        private UdpClient socket;
        private SortedDictionary<UInt32, RemoteClient> clients = new SortedDictionary<UInt32, RemoteClient>();
        private UInt32 myID;
        private NaCLKeyPair keyPair;
        private IPacketDissector adderssDissector = new AddressDissector();
        private Timer helloTask;

        private IPEndPoint stunEndpoint;

        private IPEndPoint externalIP;

        bool isRunned;

        public IPEndPoint ExternalIP { get => externalIP; set => externalIP = value; }

        public void Dispose()
        {
            isRunned = true;
            socket.Close();

            helloTask.Dispose();
        }

        private void ListenSocketThread()
        {
            helloTask = new Timer(SendHelloTimerTask, this, 1000, 1000);

            while (isRunned)
            {
                IPEndPoint from = null;
                byte[] data = socket.Receive(ref from);

                if (data.Length == 0)
                    continue;

                if(from.Equals(this.stunEndpoint))
                {
                    IPEndPoint endPoint = STUNParser.ParseSTUNResponse(data);
                    

                }

                IDAdderss adderss = (IDAdderss)adderssDissector.Dissect(data);
            }
        }

        private void SendHelloTimerTask(object state)
        {
            lock(clients)
            {
                foreach(var client in clients)
                {
                    if(!client.Value.IsConnected)
                        client.Value.PrepareHello();
                    
                    // TODO Check hello attemts
                }
            }
        }

        internal void SendTo(byte[] data, IPEndPoint to)
        {
            IDAdderss adderssPacket = new IDAdderss();
            adderssPacket.FromID = myID;
            adderssPacket.Payload = data;

            byte[] packet = adderssDissector.Assembly(adderssPacket);

            socket.Send(packet, packet.Length, to);
        }

        public void AddPeer(IPEndPoint endPoint, UInt32 peerID)
        {
            //Todo refactoe me

            clients.Add(peerID, new RemoteClient(endPoint, endPoint, peerID));
        }

        public void GetExternalIP(IPEndPoint stunServer)
        {
            byte[] data = STUNParser.CreateStunRequest(new byte[12]);
            socket.Send(data, data.Length, stunServer);

            this.stunEndpoint = stunServer;
        }

        public class Builder
        {
            PrivateNetwork network = new PrivateNetwork();
            IPEndPoint endPoint;

            public Builder()
            {

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
                network.myID = id;

                return this;
            }

            public Builder AddBindAddress(IPEndPoint endPoint)
            {
                this.endPoint = endPoint;

                return this;
            }

            public PrivateNetwork Build()
            {
                if (network.myID == 0)
                    throw new InvalidOperationException("ID required");

                // Build Socket

                if (endPoint != null)
                    network.socket = new UdpClient(endPoint);
                else
                    network.socket = new UdpClient();

                const int SIO_UDP_CONNRESET = -1744830452;
                byte[] inValue = new byte[] { 0 };
                byte[] outValue = new byte[] { 0 };
                network.socket.Client.IOControl(SIO_UDP_CONNRESET, inValue, outValue);

                network.isRunned = true;

                Thread thr = new Thread(network.ListenSocketThread);
                thr.Start();

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
