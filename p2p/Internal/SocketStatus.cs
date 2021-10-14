using System;
using System.Collections.Generic;
using System.Text;

namespace P2P.Internal
{
    internal enum SocketStatus
    {
        CLOSED,
        LISTEN,
        SYN_SENT,
        SYN_RECEIVED,
        ESTABLISHED,
        FIN_WAIT_1,
        FIN_WARI_2,
        CLOSE_WAIT,
        CLOSING,
        LAST_ACK,
        TIME_WAIT
    }
}
