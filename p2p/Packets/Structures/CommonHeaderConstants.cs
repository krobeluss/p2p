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
        public const byte DATA = 4;
    }
}
