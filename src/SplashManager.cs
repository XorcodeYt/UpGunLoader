using System;
using System.Threading;
using System.Windows.Forms;

public static class SplashManager
{
    private static Thread _uiThread;
    private static SplashScreen _form;
    private static readonly object _lock = new();

    public static void Show(string message = "Opération en cours")
    {
        lock (_lock)
        {
            if (_form != null && _form.IsHandleCreated) { Update(message); return; }

            _uiThread = new Thread(() =>
            {
                _form = new SplashScreen();
                _form.SetStatus(message);
                Application.Run(_form);
            });
            _uiThread.IsBackground = true;
            _uiThread.SetApartmentState(ApartmentState.STA);
            _uiThread.Start();
        }
    }

    public static void Update(string message)
    {
        lock (_lock)
        {
            if (_form == null) return;
            if (_form.IsHandleCreated)
                _form.BeginInvoke(new Action(() => _form.SetStatus(message)));
        }
    }

    public static void Close()
    {
        Thread threadToJoin = null;
        SplashScreen form = null;

        lock (_lock)
        {
            if (_form == null) return;

            form = _form;
            threadToJoin = _uiThread;

            _form = null;
            _uiThread = null;
        }

        try
        {
            if (form.IsHandleCreated && !form.IsDisposed)
            {
                form.BeginInvoke(new Action(() =>
                {
                    try { form.Close(); } catch { }
                    try { form.Dispose(); } catch {  }
                    try { Application.ExitThread(); } catch {  }
                }));
            }
            else
            {
                try { form.Dispose(); } catch {  }
            }
        }
        catch
        {
            try { form.Dispose(); } catch {  }
        }

        if (threadToJoin != null && threadToJoin.IsAlive)
        {
            try { threadToJoin.Join(2000); } catch {  }
        }
    }

    public static IDisposable Scope(string initialMessage = "Opération en cours")
    {
        Show(initialMessage);
        return new ScopeImpl();
    }

    private sealed class ScopeImpl : IDisposable
    {
        public void Dispose() => Close();
    }
}