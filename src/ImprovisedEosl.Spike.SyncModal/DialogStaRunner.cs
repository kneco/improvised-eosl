using System.Windows.Threading;
using ImprovisedEosl.Core;

namespace ImprovisedEosl.Spike.SyncModal;

public static class DialogStaRunner
{
    public static string Run(DialogRequest request, TimeSpan timeout)
    {
        using var completed = new ManualResetEventSlim(false);
        string result = "undefined";
        Exception? failure = null;
        DialogWindow? dialogWindow = null;
        Dispatcher? dialogDispatcher = null;

        var thread = new Thread(() =>
        {
            try
            {
                request.Log($"child STA thread started: {Environment.CurrentManagedThreadId}");
                var window = new DialogWindow(request);
                dialogWindow = window;
                dialogDispatcher = Dispatcher.CurrentDispatcher;
                window.AttachOwnerWindow();
                window.Closed += (_, _) =>
                {
                    result = window.SerializedReturnValue;
                    request.Log(
                        $"child window closed on thread {Environment.CurrentManagedThreadId}; " +
                        $"result={JsonPayloadPolicy.Summarize(result)}");
                    completed.Set();
                    Dispatcher.CurrentDispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
                };
                window.Show();
                Dispatcher.Run();
            }
            catch (Exception ex)
            {
                failure = ex;
                completed.Set();
            }
            finally
            {
                dialogWindow?.RestoreOwnerWindow();
            }
        });

        thread.Name = "ImprovisedEosl child WebView2 STA";
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();

        if (!completed.Wait(timeout))
        {
            request.Log($"child dialog timed out after {timeout.TotalSeconds:n0}s");
            if (dialogWindow is not null && dialogDispatcher is not null)
            {
                dialogDispatcher.BeginInvoke(() =>
                {
                    dialogWindow.SerializedReturnValue = "{\"kind\":\"timeout\",\"ok\":false}";
                    dialogWindow.Close();
                });

                if (!completed.Wait(TimeSpan.FromSeconds(5)))
                {
                    request.Log("child dialog timeout cleanup did not complete within 5s; restoring native owner defensively");
                    dialogWindow.RestoreOwnerWindow();
                }
            }
            return "{\"kind\":\"timeout\",\"ok\":false}";
        }

        if (failure is not null)
        {
            request.Log("child dialog failed: " + failure);
            return "{\"kind\":\"failure\",\"ok\":false}";
        }

        return result;
    }
}
