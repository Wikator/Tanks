using System.Collections.Concurrent;
using System.Threading;

namespace LiteNetLib
{
    internal abstract class BaseChannel
    {
        protected readonly ConcurrentQueue<NetPacket> OutgoingQueue;
        protected readonly NetPeer Peer;
        private int _isAddedToPeerChannelSendQueue;

        protected BaseChannel(NetPeer peer)
        {
            Peer = peer;
            OutgoingQueue = new ConcurrentQueue<NetPacket>();
        }

        public int PacketsInQueue => OutgoingQueue.Count;

        public void AddToQueue(NetPacket packet)
        {
            OutgoingQueue.Enqueue(packet);
            AddToPeerChannelSendQueue();
        }

        protected void AddToPeerChannelSendQueue()
        {
            if (Interlocked.CompareExchange(ref _isAddedToPeerChannelSendQueue, 1, 0) == 0)
                Peer.AddToReliableChannelSendQueue(this);
        }

        public bool SendAndCheckQueue()
        {
            var hasPacketsToSend = SendNextPackets();
            if (!hasPacketsToSend)
                Interlocked.Exchange(ref _isAddedToPeerChannelSendQueue, 0);

            return hasPacketsToSend;
        }

        protected abstract bool SendNextPackets();
        public abstract bool ProcessPacket(NetPacket packet);
    }
}