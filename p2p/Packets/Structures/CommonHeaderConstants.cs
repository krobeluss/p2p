using System;
using System.Collections.Generic;
using System.Text;

namespace P2P.Packets.Structures
{
    internal class CommonHeaderConstants
    {
        public const byte HELLO = 1;
        public const byte PING = 2;
        public const byte PONG = 3;
        public const byte UDP_DATA = 4;
        public const byte TCP_DATA = 5;
    }
}
