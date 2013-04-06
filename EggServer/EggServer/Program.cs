using EggServer.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
            SocketServer server = new SocketServer(100, 100);
            server.Init();
            server.Start(new IPEndPoint(IPAddress.Any, 4444));

            Console.WriteLine("Press any key to terminate the server process....");
            Console.ReadKey();
        }
    }
}
