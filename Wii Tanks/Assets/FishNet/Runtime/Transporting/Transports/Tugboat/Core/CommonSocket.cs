using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using FishNet.Utility.Performance;
using LiteNetLib;

namespace FishNet.Transporting.Tugboat
{
    public abstract class CommonSocket
    {
        #region Protected.

        /// <summary>
        ///     Transport controlling this socket.
        /// </summary>
        protected Transport Transport = null;

        #endregion


        /// <summary>
        ///     Sends data to connectionId.
        /// </summary>
        internal void Send(ref Queue<Packet> queue, byte channelId, ArraySegment<byte> segment, int connectionId,
            int mtu)
        {
            if (GetConnectionState() != LocalConnectionState.Started)
                return;

            //ConnectionId isn't used from client to server.
            var outgoing = new Packet(connectionId, segment, channelId, mtu);
            queue.Enqueue(outgoing);
        }

        /// <summary>
        ///     Updates the timeout for NetManager.
        /// </summary>
        protected void UpdateTimeout(NetManager netManager, int timeout)
        {
            if (netManager == null)
                return;

            timeout = timeout == 0 ? int.MaxValue : Math.Min(int.MaxValue, timeout * 1000);
            netManager.DisconnectTimeout = timeout;
        }

        /// <summary>
        ///     Clears a queue using Packet type.
        /// </summary>
        /// <param name="queue"></param>
        internal void ClearPacketQueue(ref ConcurrentQueue<Packet> queue)
        {
            while (queue.TryDequeue(out var p))
                p.Dispose();
        }

        /// <summary>
        ///     Clears a queue using Packet type.
        /// </summary>
        /// <param name="queue"></param>
        internal void ClearPacketQueue(ref Queue<Packet> queue)
        {
            var count = queue.Count;
            for (var i = 0; i < count; i++)
            {
                var p = queue.Dequeue();
                p.Dispose();
            }
        }

        /// <summary>
        ///     Called when data is received.
        /// </summary>
        internal virtual void Listener_NetworkReceiveEvent(Queue<Packet> queue, NetPeer fromPeer,
            NetPacketReader reader, DeliveryMethod deliveryMethod, int mtu)
        {
            //Set buffer.
            var dataLen = reader.AvailableBytes;
            //Prefer to max out returned array to mtu to reduce chance of resizing.
            var arraySize = Math.Max(dataLen, mtu);
            var data = ByteArrayPool.Retrieve(arraySize);
            reader.GetBytes(data, dataLen);
            //Id.
            var id = fromPeer.Id;
            //Channel.
            var channel = deliveryMethod == DeliveryMethod.Unreliable
                ? (byte)Channel.Unreliable
                : (byte)Channel.Reliable;
            //Add to packets.
            var packet = new Packet(id, data, dataLen, channel);
            queue.Enqueue(packet);
            //Recycle reader.
            reader.Recycle();
        }

        #region Public.

        /// <summary>
        ///     Current ConnectionState.
        /// </summary>
        private LocalConnectionState _connectionState = LocalConnectionState.Stopped;

        /// <summary>
        ///     Returns the current ConnectionState.
        /// </summary>
        /// <returns></returns>
        internal LocalConnectionState GetConnectionState()
        {
            return _connectionState;
        }

        /// <summary>
        ///     Sets a new connection state.
        /// </summary>
        /// <param name="connectionState"></param>
        protected void SetConnectionState(LocalConnectionState connectionState, bool asServer)
        {
            //If state hasn't changed.
            if (connectionState == _connectionState)
                return;

            _connectionState = connectionState;
            if (asServer)
                Transport.HandleServerConnectionState(new ServerConnectionStateArgs(connectionState, Transport.Index));
            else
                Transport.HandleClientConnectionState(new ClientConnectionStateArgs(connectionState, Transport.Index));
        }

        #endregion
    }
}