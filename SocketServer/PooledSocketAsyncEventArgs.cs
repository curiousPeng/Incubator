﻿using System;
using System.Net.Sockets;

namespace Incubator.Network
{
    public sealed class PooledSocketAsyncEventArgs : SocketAsyncEventArgs, IPooledWapper
    {
        private bool _disposed;
        private ObjectPool<IPooledWapper> _pool;
        public DateTime LastGetTime { set; get; }
        public bool IsDisposed { get { return _disposed; } }

        public PooledSocketAsyncEventArgs(ObjectPool<IPooledWapper> pool)
        {
            if (pool == null)
                throw new ArgumentNullException("pool");
            _pool = pool;
            _disposed = false;
        }

        ~PooledSocketAsyncEventArgs()
        {
            //必须为false
            Dispose(false);
        }

        public new void Dispose()
        {
            // 必须为true
            Dispose(true);
            // 通知垃圾回收机制不再调用终结器（析构器）
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            if (disposing)
            {
                // 清理托管资源
                if (_pool.IsDisposed)
                {
                    base.Dispose();
                }
                else
                {
                    _pool.Put(this);
                }
            }

            // 清理非托管资源

            // 让类型知道自己已经被释放
            _disposed = true;
        }
    }
}
