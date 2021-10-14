using P2P.Internal;
using P2P.Packets.Structures.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace P2P
{

    public class VirtualTcpClient : IVirtualTcpSocket
    {

        private static bool SEQ_LT(uint a, uint b)
        {
            return ((int)((a) - (b)) < 0);
        }

        private static bool SEQ_LEQ(uint a, uint b)
        {
            return ((int)((a) - (b)) <= 0);
        }

        private static bool SEQ_GT(uint a, uint b)
        {
            return ((int)((a) - (b)) > 0);
        }

        private static bool SEQ_GEQ(uint a, uint b)
        {
            return ((int)((a) - (b)) >= 0);
        }

        internal UInt16 localPort;

        internal VirtualEndpoint remoteEndpoint;

        internal SocketStatus status = SocketStatus.CLOSED;

        private PrivateNetwork network;

        private uint? seqCounter;
        private uint? ackCounter;

        private byte[] outBuffer = new byte[15];
        private byte[] inBuffer = new byte[1024 * 1024 * 10];

        private uint appReadPosition;
        private uint bufferStartSeq;

        private List<Segment> segments = new List<Segment>();

        private Mutex socketMutex = new Mutex( );

        internal uint? SeqCounter
        {
            get => seqCounter; set
            {
                if(seqCounter == null)
                { 
                    seqCounter = value;
                    bufferStartSeq = value.Value;
                }
            }
        }
        internal uint? AckCounter
        {
            get => ackCounter; set
            {
                if (ackCounter == null)
                {
                    ackCounter = value;
                    appReadPosition = value.Value;
                }
            }
        }

        public void Send(byte[] data)
        {
            if(status != SocketStatus.ESTABLISHED)
                throw new Exception("Ты че псина");

            // Сжимаем исходящий буффер

            CutSendedData();

            Console.WriteLine(seqCounter.Value - bufferStartSeq);
            Console.WriteLine((uint)outBuffer.Length);

            uint acceptedDataLength = Math.Min((uint)data.Length, (uint)outBuffer.Length - (seqCounter.Value - bufferStartSeq));

            Array.Copy(data, 0, this.outBuffer, seqCounter.Value - bufferStartSeq, acceptedDataLength);

            int MSS = 10;

            uint endSeq = seqCounter.Value + acceptedDataLength;

            for (uint startSeq = seqCounter.Value; SEQ_LT(seqCounter.Value, endSeq); )
            {
                TcpData packet = new TcpData();

                packet.AckFlag = true;
                packet.Ack = ackCounter.Value;

                packet.Seq = seqCounter.Value;

                int segmentLength = (int)Math.Min(MSS, endSeq - seqCounter.Value);

                packet.Payload = new byte[segmentLength];

                Array.Copy(this.outBuffer, (uint)(seqCounter.Value - bufferStartSeq), packet.Payload, 0, segmentLength);

                AppendPorts(packet);
                RegisterRetransmitablePacket(packet, segmentLength);

                network.SendVirtualTcpPacket(packet, remoteEndpoint.Id);

                seqCounter = (uint?)(seqCounter.Value + segmentLength);

            }

            //seqCounter += (uint?)data.Length;
        }

        private void CutSendedData( )
        {
            socketMutex.WaitOne();

            segments.Sort((x, y) =>
            {
                if (SEQ_GT(x.seq, y.seq))
                    return 1;
                if (SEQ_LT(x.seq, y.seq))
                    return -1;

                return 0;
            });

            socketMutex.ReleaseMutex();
        }

        internal VirtualTcpClient(ushort localPort, VirtualEndpoint remoteEndpoint, PrivateNetwork network)
        {
            this.localPort = localPort;
            this.remoteEndpoint = remoteEndpoint;
            this.network = network;
        }

        internal void ProcessFrame(TcpData data)
        {
            throw new NotImplementedException();
        }

        public void Connect()
        {
            if (status == SocketStatus.CLOSED)
            {
                status = SocketStatus.SYN_SENT;
                SendSyn();
            }
                
        }

        private void AppendPorts(TcpData data)
        {
            data.FromPort = localPort;
            data.ToPort = (ushort)remoteEndpoint.Port;
        }

        internal void SendSyn()
        {
            if(seqCounter == null)
            {
                SeqCounter = SynGenerator.GetNextSeq( );

                TcpData data = new TcpData();

                if(ackCounter != null)
                {
                    data.Ack = ackCounter.Value;
                    data.AckFlag = true;
                }

                data.SynFlag = true;
                data.Seq = seqCounter.Value;

                AppendPorts(data);

                RegisterRetransmitablePacket(data);
                seqCounter++;
                bufferStartSeq++;

                network.SendVirtualTcpPacket(data, remoteEndpoint.Id);
            }
        }
        void IVirtualTcpSocket.ProcessFrame(TcpData data, VirtualEndpoint from )
        {
            if( status == SocketStatus.SYN_SENT )
            {
                if (data.SynFlag && data.AckFlag)
                {
                    ackCounter = data.Seq + 1;

                    status = SocketStatus.ESTABLISHED;

                    TcpData answer = new TcpData();
                    answer.Seq = seqCounter.Value;
                    answer.AckFlag = true;
                    answer.Ack = ackCounter.Value;

                    AppendPorts(answer);

                    network.SendVirtualTcpPacket(answer, remoteEndpoint.Id);
                }
                else if(data.SynFlag)
                {
                    ackCounter = data.Seq + 1;
                    status = SocketStatus.SYN_RECEIVED;

                    SendSyn();
                }
            }
            else if(status == SocketStatus.SYN_RECEIVED)
            {
                if(data.AckFlag)
                    status = SocketStatus.ESTABLISHED;
            }

            if (data.AckFlag)
                AcknowledgeSegments(data.Ack);
        }

        private void RegisterRetransmitablePacket(TcpData data)
        {
            RegisterRetransmitablePacket(data, 0);
        }

        private void AcknowledgeSegments( uint ack )
        {

            return;

            for( int i = 0; i < segments.Count; )
            {
                Segment s = segments[i];

                if(s.endSeq <= ack)
                {
                    // Sehment fully received

                    // TODO Buffer manipulation

                    segments.RemoveAt(i);
                    break;
                }   
            }
        }
        private void RegisterRetransmitablePacket(TcpData data, int length)
        {
            Segment s = new Segment();

            s.finSet = data.FinFlag;
            s.synSet = data.SynFlag;

            s.length = length;

            s.seq = data.Seq;

            s.endSeq = (uint)(s.seq + length);
            s.endSeq += s.finSet ? 1 : 0;
            s.endSeq += s.synSet ? 1 : 0;

            s.sendTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            segments.Add(s);
        }

        class Segment
        {
            public int length;

            public uint seq;

            public bool synSet;

            public bool finSet;

            public long sendTime;

            public uint endSeq;
        }
    }
}
