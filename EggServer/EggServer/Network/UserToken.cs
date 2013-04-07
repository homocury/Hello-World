using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EggServer.Network
{
    class UserToken
    {
        public Socket Socket { get; set; }
        public SocketAsyncEventArgs SendSAEA { get; set; }
        public SocketAsyncEventArgs RecvSAEA { get; set; }

        public int mRecvBufferOffset;

        // 게임 유저를 여기에 붙인다.
        public object UserObject { get; set; }

        public void Reset()
        {
            Socket = null;
            UserObject = null;
        }
    }
}
