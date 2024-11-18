using DiscordRPC;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace StarRailDiscordRpc;

internal static class Program
{
    private const string Impact = "1113930060513153096";

    [STAThread]
    static void Main()
    {
        using var self = new Mutex(true, "Honkai DiscordRPC", out var allow);
        if (!allow)
        {
            MessageBox.Show("Honkai DiscordRPC is already running.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.Exit(-1);
        }

        if (Properties.Settings.Default.IsFirstTime)
        {
            AutoStart.Set();
            Properties.Settings.Default.IsFirstTime = false;
            Properties.Settings.Default.Save();
        }

        Task.Run(async () =>
        {
            using var clientImp = new DiscordRpcClient(Impact);
            clientImp.Initialize();

            var isPlaying = false;
			var delay = 1000;

			while (true)
			{
				await Task.Delay(delay);

				Debug.Print($"InLoop");

				var isMiHoYo = true;
				var handle = FindWindow("UnityWndClass", "Honkai Impact 3rd");

				if (handle == IntPtr.Zero)
				{
					var bh3Process = Process.GetProcessesByName("bh3").FirstOrDefault();
					if (bh3Process != null)
					{
                        isMiHoYo = true;
						handle = bh3Process.MainWindowHandle;
					}
				}

				if (handle == IntPtr.Zero)
				{
					Debug.Print($"Not found game process.");
					isPlaying = false;
                    delay = 1000;

					if (clientImp.CurrentPresence != null)
					{
						clientImp.ClearPresence();
					}
					continue;
				}

				try
                {
                    var process = Process.GetProcesses().First(x => x.MainWindowHandle == handle);

                    Debug.Print($"Check process with {handle} | {process.ProcessName}");

                    if (isMiHoYo)
                    {
                        if (!isPlaying)
                        {
                            isPlaying = true;
                            delay = 30000;
							clientImp.UpdateRpc("iconx", "Honkai Impact 3rd");
                            Debug.Print($"Set RichPresence to {process.ProcessName}");
                        }
                        else
                        {
                            Debug.Print($"Keep RichPresence to {process.ProcessName}");
                        }
                    }
                }
                catch (Exception e)
                {
                    isPlaying = false;
                    if (clientImp.CurrentPresence != null)
                    {
                        clientImp.ClearPresence();
                    }
                    Debug.Print($"{e.Message}{Environment.NewLine}{e.StackTrace}");
                }
            }
        });

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var notifyMenu = new ContextMenu();
        var exitButton = new MenuItem("Exit");
        var autoButton = new MenuItem("AutoStart" + "    " + (AutoStart.Check() ? "√" : "✘"));
        notifyMenu.MenuItems.Add(0, autoButton);
        notifyMenu.MenuItems.Add(1, exitButton);

        var notifyIcon = new NotifyIcon()
        {
            BalloonTipIcon = ToolTipIcon.Info,
            ContextMenu = notifyMenu,
            Text = "Honkai DiscordRPC",
            Icon = Properties.Resources.tray,
            Visible = true,
        };

        exitButton.Click += (_, _) =>
        {
            notifyIcon.Visible = false;
            Thread.Sleep(100);
            Environment.Exit(0);
        };
        autoButton.Click += (_, _) =>
        {
            if (AutoStart.Check())
            {
                AutoStart.Remove();
            }
            else
            {
                AutoStart.Set();
            }

            autoButton.Text = "AutoStart" + "    " + (AutoStart.Check() ? "√" : "✘");
        };


        Application.Run();
    }

    private static void UpdateRpc(this DiscordRpcClient client, string key, string text)
        => client.SetPresence(new RichPresence
        {
            Assets = new Assets
            {
                LargeImageKey = key,
                LargeImageText = text,
            },
            Timestamps = Timestamps.Now,
        });

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
}
