using System;
using System.Collections.Generic;

namespace NetworkManager
{
    public abstract class NetworkMgrBase<T> : INetworkMgr<T> where T : INetworkComponent
    {
        public abstract bool IsConnected(T st, T ed);

        public abstract void OnConnect(T st);

        public abstract void OnDisconnect(T st);
    }

    public class ChannelNetworkMgrBase : NetworkMgrBase<IChannelNetworkComponent>
    {
        protected const int SpeicalChannelStart = 80_000;
        protected Dictionary<int, ChannelData> channels;

        public ChannelNetworkMgrBase() => channels = new Dictionary<int, ChannelData>();

        public override bool IsConnected(IChannelNetworkComponent st, IChannelNetworkComponent ed) => st.GetChannel == ed.GetChannel;

        public override void OnConnect(IChannelNetworkComponent st)
        {
            int ch = st.GetChannel;
            ConnectChangeEventHandler hnd = st.GetEventHandler;

            if (!channels.ContainsKey(ch))
                CreateChannel(ch);

            channels[ch].EventHandlers += hnd;
        }

        public override void OnDisconnect(IChannelNetworkComponent st)
        {
            int ch = st.GetChannel;
            ConnectChangeEventHandler hnd = st.GetEventHandler;

            if (channels.ContainsKey(ch) && hnd != null)
                channels[ch].EventHandlers -= hnd;
        }

        public virtual void OnEmitterConnect(IChannelNetworkComponentEX st)
        {
            int ch = st.GetChannel;

            if (!channels.ContainsKey(ch))
                CreateChannel(ch);

            channels[ch].Subscribe(this, st);
        }

        public virtual void OnEmitterDisconenct(IChannelNetworkComponentEX st)
        {
            int ch = st.GetChannel;

            if (channels.ContainsKey(ch))
                channels[ch].Unsubscribe(this, st);
        }
        
        // 이거 안 쓰는것 같아요
        public virtual void SetSignalEmit(IChannelNetworkComponent st)
        {
            if (st.GetEventHandler == null)
                SignalEmit(st.GetChannel, st.GetSignal);
        }

        public virtual void SignalEmit(int ch, bool ison)
        {
            if (!channels.ContainsKey(ch) || channels[ch].IsDoNotNeedUpdate(ison)) return;

            channels[ch].UpdateActivate(ison);
            channels[ch].InvokeEvent(this, ison);
        }

        public virtual void Reset()
        {
            foreach (KeyValuePair<int, ChannelData> x in channels)
                x.Value.Clear();
            channels.Clear();
            GC.Collect(0, GCCollectionMode.Forced);
        }

        public virtual bool IsChannelOn(int ch) => channels.TryGetValue(ch, out ChannelData res) ? res.IsActivate() : false;

        protected virtual void CreateChannel(int ch) =>
            channels.Add(ch, ch < 80_001 ? new ChannelData() : new OrChannelData());
    }
}
