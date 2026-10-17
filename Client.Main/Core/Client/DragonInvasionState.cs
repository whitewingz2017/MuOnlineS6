using System.Threading;

namespace Client.Main.Core.Client
{
    /// <summary>Server-driven ambient invasion state, independent of world loading.</summary>
    public sealed class DragonInvasionState
    {
        private int _event;
        public int ActiveEvent => Volatile.Read(ref _event);

        public void Apply(byte eventNumber, bool enabled)
        {
            if (eventNumber is not (1 or 3))
                return;

            if (enabled)
                Volatile.Write(ref _event, eventNumber);
            else
                Interlocked.CompareExchange(ref _event, 0, eventNumber);
        }

        public void Reset() => Volatile.Write(ref _event, 0);
    }
}
