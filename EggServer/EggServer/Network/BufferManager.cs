using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EggServer.Network
{
    class BufferManager
    {
        int mTotalBytes;
        byte[] mBuffer;
        Stack<int> mFreeIndexPool;
        int mCurrentIndex;
        int mBufferSize;

        public BufferManager(int totalBytes, int bufferSize)
        {
            mTotalBytes = totalBytes;
            mCurrentIndex = 0;
            mBufferSize = bufferSize;
            mFreeIndexPool = new Stack<int>();

            mBuffer = new byte[mTotalBytes];
        }

        public bool SetBuffer(SocketAsyncEventArgs args)
        {
            if (mFreeIndexPool.Count > 0)
            {
                args.SetBuffer(mBuffer, mFreeIndexPool.Pop(), mBufferSize);
            }
            else
            {
                if ((mTotalBytes - mBufferSize) < mCurrentIndex)
                {
                    return false;
                }

                args.SetBuffer(mBuffer, mCurrentIndex, mBufferSize);
                mCurrentIndex += mBufferSize;
            }

            return true;
        }

        // 얘가 불릴 일이 있을까? SAEA는 한번 버퍼를 설정하면 프로그램 종료때까지 그 버퍼를 유지한다.
        public void FreeBuffer(SocketAsyncEventArgs args)
        {
            mFreeIndexPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }
    }
}
