using EggServer.Network;
using EggServer.Util.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace EggServer
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var mutex = new Mutex (false, "eggmoney"))
            {
                if (!mutex.WaitOne(0))
                {
                    Console.WriteLine ("서버가 이미 실행중입니다.");
                    return;
                }

                RunProgram(args);
            }
        }

        static void RunProgram(string[] args)
        {
            int processCount = Environment.ProcessorCount;
            ThreadPool.SetMinThreads(processCount, processCount);
            ThreadPool.SetMaxThreads(processCount, processCount);

            XmlHelper config = new XmlHelper("EggServerConfig.xml");
            int maxConnections = config.GetSingleNodeValue<int>("/EggServerConfig/Network/MaxConnections");
            int listenPort = config.GetSingleNodeValue<int>("/EggServerConfig/Network/ListenPort");
            int sendBufferSize = config.GetSingleNodeValue<int>("/EggServerConfig/Network/SendBufferSize");
            int recvBufferSize = config.GetSingleNodeValue<int>("/EggServerConfig/Network/RecvBufferSize");
            int maxOutstandingAccepts = config.GetSingleNodeValue<int>("/EggServerConfig/Network/MaxOutstandingAccepts");

            SocketServer server = new SocketServer(maxConnections, maxOutstandingAccepts, sendBufferSize, recvBufferSize);
            server.Start(new IPEndPoint(IPAddress.Any, listenPort));

            Console.WriteLine("Press any key to terminate the server process....");
            Console.ReadKey();
        }
    }
}
