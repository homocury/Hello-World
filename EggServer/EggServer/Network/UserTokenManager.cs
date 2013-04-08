using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EggServer.Network
{
    class UserTokenManager
    {
        private SocketServer mServer;

        private ConcurrentStack<UserToken> mPool;

        private BufferManager mSendBufferManager;
        private BufferManager mRecvBufferManager;

        private EventHandler<SocketAsyncEventArgs> OnComplete;

        public UserTokenManager(SocketServer server, int maxConnections, int sendBufferSize, int recvBufferSize, EventHandler<SocketAsyncEventArgs> onComplete)
        {
            mServer = server;

            mPool = new ConcurrentStack<UserToken>();

            mSendBufferManager = new BufferManager(maxConnections * sendBufferSize, sendBufferSize);
            mRecvBufferManager = new BufferManager(maxConnections * recvBufferSize, recvBufferSize);

            OnComplete = onComplete;

            for (int i = 0; i < maxConnections; ++i)
            {
                Release(CreateUserToken());
            }
        }

        public void Release(UserToken userToken)
        {
            if (userToken == null) 
            { 
                throw new ArgumentNullException("UserToken cannot be null"); 
            }

            userToken.Reset();

            mPool.Push(userToken);
        }

        public UserToken New()
        {
            UserToken userToken;

            if (mPool.TryPop(out userToken))
            {
                return userToken;
            }
            else
            {
                return CreateUserToken();
            }
        }

        public int Count
        {
            get { return mPool.Count; }
        }

        private UserToken CreateUserToken()
        {
            SocketAsyncEventArgs sendEventArg;
            sendEventArg = new SocketAsyncEventArgs();
            sendEventArg.Completed += OnComplete;

            SocketAsyncEventArgs recvEventArg;
            recvEventArg = new SocketAsyncEventArgs();
            recvEventArg.Completed += OnComplete;

            // 처음 생성할 때 버퍼를 할당해 놓는다. 풀에 반납하더라도 버퍼는 유지된다.
            mSendBufferManager.SetBuffer(sendEventArg);
            mRecvBufferManager.SetBuffer(recvEventArg);

            UserToken userToken = new UserToken(mServer, sendEventArg, recvEventArg);

            sendEventArg.UserToken = userToken;
            recvEventArg.UserToken = userToken;

            return userToken;
        }
    }
}
