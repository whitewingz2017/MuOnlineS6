using System;

namespace Client.Main.Core.Utilities;

/// <summary>One outstanding NPC purchase at a time, advanced by server acknowledgements.</summary>
public sealed class NpcPurchaseBatch
{
    private readonly Action<byte> _send;
    private readonly Func<bool> _canBuy;
    private readonly Func<DateTimeOffset> _clock;
    private DateTimeOffset _deadline;
    private byte _slot;
    public int Remaining { get; private set; }
    public bool IsActive => Remaining>0;

    public NpcPurchaseBatch(Action<byte> send,Func<bool> canBuy,Func<DateTimeOffset> clock=null)
    { _send=send;_canBuy=canBuy;_clock=clock??(()=>DateTimeOffset.UtcNow); }

    public bool Start(byte slot,int quantity)
    {
        if(IsActive || !_canBuy() || quantity is <1 or >99)return false;
        _slot=slot;Remaining=quantity;SendNext();return true;
    }

    public void Acknowledge(bool success)
    {
        if(!IsActive)return;
        if(!success || !_canBuy()){Cancel();return;}
        if(--Remaining>0)SendNext();
    }

    private void SendNext()
    {
        _deadline=_clock().AddSeconds(10);
        try{_send(_slot);}catch{Cancel();throw;}
    }

    public bool CheckTimeout()
    {
        if(!IsActive || _clock()<_deadline)return false;
        Cancel();return true;
    }

    public void Cancel()=>Remaining=0;
}
