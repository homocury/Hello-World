using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EggServer.Network
{
    class SocketServer
    {
        private int mMaxConnections;
        private int mMaxAccepts;

        UserTokenManager mUserTokenManager;

        Socket mListenSocket;

        int mTotalBytesRead;
        int mNumConnectedSockets;
        Semaphore mMaxNumberAcceptedClients;

        public SocketServer(int numConnections, int numAccepts, int sendBufferSize, int recvBufferSize)
        {
            mTotalBytesRead = 0;
            mNumConnectedSockets = 0;
            mMaxConnections = numConnections;
            mMaxAccepts = numAccepts;

            mUserTokenManager = new UserTokenManager(this, numConnections, sendBufferSize, recvBufferSize, 
                new EventHandler<SocketAsyncEventArgs>(IO_Completed));

            mMaxNumberAcceptedClients = new Semaphore(numConnections, numConnections);
        }

        private SocketAsyncEventArgs CreateAcceptSAEA()
        {
            // accept 소켓풀 생성
            //
            //TODO: 나중에 소켓 재사용 하도록 고쳐야 한다.

            SocketAsyncEventArgs acceptEventArg;
            acceptEventArg = new SocketAsyncEventArgs();
            acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            return acceptEventArg;
        }

        public void Start(IPEndPoint localEndPoint)
        {
            mListenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            mListenSocket.Bind(localEndPoint);
            mListenSocket.Listen(1000);

            for (int i = 0; i < mMaxAccepts; ++i)
                StartAccept(null);
        }

        public void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            if (acceptEventArg == null)
                acceptEventArg = CreateAcceptSAEA();

            // 최대 동접 유지
            mMaxNumberAcceptedClients.WaitOne();
            
            if (!mListenSocket.AcceptAsync(acceptEventArg))
            {
                // AcceptAsync 가 즉시 완료된 경우 SocketAsyncEventArgs.Completed 가 콜 되지 않기 때문에 직접 처리해준다.
                // 하지만 이 방법이 좋은지는 의문. 최악의 경우 스택이 터질수도 있다.
                ProcessAccept(acceptEventArg);
            }
        }

        void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            Interlocked.Increment(ref mNumConnectedSockets);
            Console.WriteLine("Client connection accepted. There are {0} clients connected to the server",
                mNumConnectedSockets);

            UserToken userToken = mUserTokenManager.New();

            // nagle 꺼준다.
            e.AcceptSocket.NoDelay = true;

            userToken.Socket = e.AcceptSocket;
            
            // saea에서 넘어온 소켓은 초기화해준다. 나중에 소켓 재사용하게 되면 수정해야 한다.
            e.AcceptSocket = null;

            if (!userToken.Socket.ReceiveAsync(userToken.RecvSAEA))
            {
                ProcessReceive(userToken.RecvSAEA);
            }

            StartAccept(e);
        }

        void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }       
        }

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            UserToken token = e.UserToken as UserToken;
          
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                Interlocked.Add(ref mTotalBytesRead, e.BytesTransferred);
                
                Console.WriteLine("The server has read a total of {0} bytes", mTotalBytesRead);

                // 지금까지 받은 전체 bytes
                token.RecvBufferOffset += e.BytesTransferred;

                int startIndex = 0;

                while (true)
                {
                    // 헤더가 있는지 확인
                    if (token.RecvBufferOffset < 8)
                    {
                        break;
                    }

                    int packetLength = BitConverter.ToInt32(e.Buffer, startIndex + 4);
                    if (token.RecvBufferOffset < 8 + packetLength)
                    {
                        break;
                    }

                    // 패킷이 다 받아졌다.
                    
                    //TODO: 핸들러를 호출하자.

                    startIndex += (8 + packetLength);
                    token.RecvBufferOffset -= (8 + packetLength);
                }

                // 버퍼 오프셋 세팅
                e.SetBuffer(e.Offset + token.RecvBufferOffset, e.Count - token.RecvBufferOffset);
                
                // 재수 없으면 스택이 터질수도 있다.
                if (!token.Socket.ReceiveAsync(e))
                {
                    ProcessReceive(e);
                }
            }
            else
            {
                CloseClientSocket(e);
            }
        }

        public void StartSend(SocketAsyncEventArgs e)
        {
            UserToken token = e.UserToken as UserToken;

            if (!token.Socket.SendAsync(e))
            {
                ProcessSend(e);
            }
        }

        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                //TODO: UserToken 뒤져서 Send 버퍼에 쌓인게 있는지 확인하고 계속 보낸다.

                UserToken token = e.UserToken as UserToken;

                if (!token.FlushSendData())
                {
                    token.Sending = 0;
                }
            }
            else
            {
                CloseClientSocket(e);
            }
        }

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            UserToken token = e.UserToken as UserToken;

            try
            {
                token.Socket.Shutdown(SocketShutdown.Send);
            }
            catch (Exception)
            {
                // 이미 닫힌 경우
            }

            token.Socket.Close();

            Interlocked.Decrement(ref mNumConnectedSockets);
            
            mMaxNumberAcceptedClients.Release();

            Console.WriteLine("A client has been disconnected from the server. There are {0} clients connected to the server", mNumConnectedSockets);

            mUserTokenManager.Release(token);
        }
    }
}
