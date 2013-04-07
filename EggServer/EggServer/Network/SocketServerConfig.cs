using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EggServer.Network
{
    class SocketServerConfig
    {
        public int MaxConnections { get; set; }
        public int Port { get; set; }
        public int BackLog { get; set; }

        public SocketServerConfig(int port, int maxConnections, int backLog)
        {
            Port = port;
            MaxConnections = maxConnections;
            BackLog = backLog;
        }
    }
}
