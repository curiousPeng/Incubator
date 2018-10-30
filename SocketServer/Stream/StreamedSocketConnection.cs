﻿using System;
using System.Buffers;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Incubator.SocketServer
{
    public abstract class StreamedSocketConnection : ConnectionBase
    {
        int _id;
        int _position;
        bool _disposed;
        byte[] _largebuffer;
        SocketAwaitable _readAwait;
        SocketAwaitable _sendAwait;

        public StreamedSocketConnection(int id, Socket socket, BaseListener listener, bool debug)
            : base(id, socket, listener, debug)
        {
            _position = 0;
            _pooledReadEventArgs = _socketListener.SocketAsyncReceiveEventArgsPool.Get() as PooledSocketAsyncEventArgs;
            _readEventArgs = _pooledReadEventArgs.SocketAsyncEvent;

            _pooledSendEventArgs = _socketListener.SocketAsyncSendEventArgsPool.Get() as PooledSocketAsyncEventArgs;
            _sendEventArgs = _pooledSendEventArgs.SocketAsyncEvent;

            _readAwait = new SocketAwaitable(_readEventArgs);
            _sendAwait = new SocketAwaitable(_sendEventArgs);
        }

        ~StreamedSocketConnection()
        {
            //必须为false
            Dispose(false);
        }

        private async Task FillBuffer(int count)
        {
            var read = 0;
            do
            {
                _readEventArgs.SetBuffer(read, count - read);
                await _socket.ReceiveAsync(_readAwait);
                if (_readEventArgs.BytesTransferred == 0)
                {
                    // FIN here
                    break;
                }
            }
            while ((read += _readEventArgs.BytesTransferred) < count);
            _position = read;
        }

        private async Task FillLargeBuffer(int count)
        {
            var read = 0;
            ReleaseLargeBuffer();
            _largebuffer = ArrayPool<byte>.Shared.Rent(count);
            do
            {
                await _socket.ReceiveAsync(_readAwait);
                if (_readEventArgs.BytesTransferred == 0)
                {
                    // FIN here
                    break;
                }
                Buffer.BlockCopy(_readEventArgs.Buffer, 0, _largebuffer, read, _readEventArgs.BytesTransferred);
            }
            while ((read += _readEventArgs.BytesTransferred) < count);
        }

        private void ReleaseLargeBuffer()
        {
            if (_largebuffer != null)
            {
                ArrayPool<byte>.Shared.Return(_largebuffer, true);
                _largebuffer = null;
            }
        }

        public async Task<int> ReadInt32()
        {
            await FillBuffer(4);
            return (int)(_readEventArgs.Buffer[0] | _readEventArgs.Buffer[1] << 8 | _readEventArgs.Buffer[2] << 16 | _readEventArgs.Buffer[3] << 24);
        }

        public async Task<ArraySegment<byte>> ReadBytes(int count)
        {
            if (count > _socketListener.BufferSize)
            {
                await FillLargeBuffer(count);
                return _largebuffer;
            }
            else
            {
                await FillBuffer(count);
                return new ArraySegment<byte>(_readEventArgs.Buffer, _position, count);
            }
        }

        public async Task Write(byte[] buffer, int offset, int count, bool rentFromPool)
        {
            if (count > _socketListener.BufferSize)
            {
                var remain = count;
                while (remain > 0)
                {
                    _sendEventArgs.SetBuffer(offset, remain);
                    Buffer.BlockCopy(buffer, 0, _sendEventArgs.Buffer, offset, count);
                    await _socket.SendAsync(_sendAwait);
                }
            }
            else
            {

            }

            if (rentFromPool)
            {
                ArrayPool<byte>.Shared.Return(buffer, true);
            }
        }

        public async Task Write(bool value)
        {
            _sendEventArgs.SetBuffer(0, 1);
            _sendEventArgs.Buffer[0] = (byte)(value ? 1 : 0);
            await _socket.SendAsync(_sendAwait);
        }

        public async Task Write(byte value)
        {
            _sendEventArgs.SetBuffer(0, 1);
            _sendEventArgs.Buffer[0] = value;
            await _socket.SendAsync(_sendAwait);
        }

        private unsafe void UnsafeDoubleBytes(double value)
        {
            ulong TmpValue = *(ulong*)&value;
            _sendEventArgs.Buffer[0] = (byte)TmpValue;
            _sendEventArgs.Buffer[1] = (byte)(TmpValue >> 8);
            _sendEventArgs.Buffer[2] = (byte)(TmpValue >> 16);
            _sendEventArgs.Buffer[3] = (byte)(TmpValue >> 24);
            _sendEventArgs.Buffer[4] = (byte)(TmpValue >> 32);
            _sendEventArgs.Buffer[5] = (byte)(TmpValue >> 40);
            _sendEventArgs.Buffer[6] = (byte)(TmpValue >> 48);
            _sendEventArgs.Buffer[7] = (byte)(TmpValue >> 56);
        }

        public async Task Write(double value)
        {
            _sendEventArgs.SetBuffer(0, 8);
            UnsafeDoubleBytes(value);
            await _socket.SendAsync(_sendAwait);
        }

        public async Task Write(short value)
        {
            _sendEventArgs.SetBuffer(0, 2);
            _sendEventArgs.Buffer[0] = (byte)value;
            _sendEventArgs.Buffer[1] = (byte)(value >> 8);
            await _socket.SendAsync(_sendAwait);
        }

        public async Task Write(int value)
        {
            _sendEventArgs.SetBuffer(0, 4);
            _sendEventArgs.Buffer[0] = (byte)value;
            _sendEventArgs.Buffer[1] = (byte)(value >> 8);
            _sendEventArgs.Buffer[2] = (byte)(value >> 16);
            _sendEventArgs.Buffer[3] = (byte)(value >> 24);
            await _socket.SendAsync(_sendAwait);
        }

        public async Task Write(long value)
        {
            _sendEventArgs.SetBuffer(0, 8);
            _sendEventArgs.Buffer[0] = (byte)value;
            _sendEventArgs.Buffer[1] = (byte)(value >> 8);
            _sendEventArgs.Buffer[2] = (byte)(value >> 16);
            _sendEventArgs.Buffer[3] = (byte)(value >> 24);
            _sendEventArgs.Buffer[4] = (byte)(value >> 32);
            _sendEventArgs.Buffer[5] = (byte)(value >> 40);
            _sendEventArgs.Buffer[6] = (byte)(value >> 48);
            _sendEventArgs.Buffer[7] = (byte)(value >> 56);
            await _socket.SendAsync(_sendAwait);
        }

        private unsafe void UnsafeFloatBytes(float value)
        {
            uint TmpValue = *(uint*)&value;
            _sendEventArgs.Buffer[0] = (byte)TmpValue;
            _sendEventArgs.Buffer[1] = (byte)(TmpValue >> 8);
            _sendEventArgs.Buffer[2] = (byte)(TmpValue >> 16);
            _sendEventArgs.Buffer[3] = (byte)(TmpValue >> 24);
        }

        public async Task Write(float value)
        {
            _sendEventArgs.SetBuffer(0, 1);
            UnsafeFloatBytes(value);
            await _socket.SendAsync(_sendAwait);
        }

        public async Task Write(decimal value)
        {
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            if (disposing)
            {
                // 清理托管资源
                _sendEventArgs.UserToken = null;
                _readEventArgs.UserToken = null;
            }

            // 清理非托管资源

            // 让类型知道自己已经被释放
            _disposed = true;
            base.Dispose();
        }
    }
}