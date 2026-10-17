using System;
using System.Threading;
using System.Threading.Tasks;

namespace Client.Main.Helpers;

public static class AppUpdatePoller
{
    public static async Task RunAsync(Func<bool,Task> check,TimeSpan interval,CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await check(true).ConfigureAwait(false);
        while(true)
        {
            await Task.Delay(interval,cancellationToken).ConfigureAwait(false);
            await check(false).ConfigureAwait(false);
        }
    }
}
