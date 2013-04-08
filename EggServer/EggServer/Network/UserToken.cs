using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;

namespace EggServer.Network
{
    class UserToken
    {
        public SocketServer Server { get; set; }
        public Socket Socket { get; set; }
        public SocketAsyncEventArgs SendSAEA { get; private set; }
        public SocketAsyncEventArgs RecvSAEA { get; private set; }

        public int RecvBufferOffset { get; set; }

        public ConcurrentQueue<byte[]> mSendDataQueue;
        public int Sending;

        // 게임 유저를 여기에 붙인다.
        public object UserObject { get; set; }

        public UserToken(SocketServer server, SocketAsyncEventArgs sendSAEA, SocketAsyncEventArgs recvSAEA)
        {
            Server = server;

            SendSAEA = sendSAEA;
            RecvSAEA = recvSAEA;

            RecvBufferOffset = 0;

            mSendDataQueue = new ConcurrentQueue<byte[]>();
            Sending = 0;
        }

        public void Reset()
        {
            Socket = null;
            UserObject = null;

            byte[] item;
            while (mSendDataQueue.TryDequeue(out item)) { }
            Sending = 0;
        }

        public void SendPacket(Int32 id, byte[] data)
        {
            // 복사를 몇 번을 하는건지... ㅠㅠ

            byte[] packetId = BitConverter.GetBytes(id);
            byte[] packetLength = BitConverter.GetBytes(data.Length);
            byte[] newPacket = new byte[8 + data.Length];

            Buffer.BlockCopy(packetId, 0, newPacket, 0, 4);
            Buffer.BlockCopy(packetLength, 0, newPacket, 4, 4);
            Buffer.BlockCopy(data, 0, newPacket, 8, data.Length);

            mSendDataQueue.Enqueue(newPacket);

            if (0 == Interlocked.CompareExchange(ref Sending, 1, 0))
            {
                FlushSendData();
            }
        }

        public bool FlushSendData()
        {
            int bytesToSend = 0;

            while (true)
            {
                byte[] item = null;

                //TODO: 버퍼 오퍼플로 체크해서 더 이상 보내지 말아야 한다.
                if (mSendDataQueue.TryDequeue(out item))
                {
                    Buffer.BlockCopy(item, 0, SendSAEA.Buffer, bytesToSend, item.Length);
                    bytesToSend += item.Length;
                }
                else
                {
                    break;
                }
            }

            if (bytesToSend > 0)
            {
                Server.StartSend(SendSAEA);
                return true;
            }

            return false;
        }
    }
}
