using System;
using System.Threading;

namespace Client.Main.Helpers;

public sealed record AppUpdateOffer(string Version,string Notes,long FileSize,Action UpdateNow);

public static class AppUpdateNotice
{
    private static AppUpdateOffer _current;
    public static AppUpdateOffer Current=>Volatile.Read(ref _current);
    public static void Publish(AppUpdateOffer offer)=>Volatile.Write(ref _current,offer);
    public static void Clear()=>Volatile.Write(ref _current,null);
}
