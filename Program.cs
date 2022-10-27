using System;
using System.Windows.Forms;
using System.Threading;
using System.Drawing;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.IO;

namespace TrayIconForDiscordWeb {
	static class Program {
        static Dictionary<string, Icon> icons = new Dictionary<string, Icon>();
        static Icon defaultIcon;

        static NotifyIcon trayIcon;

        static HttpListener server;
        static Thread httpThread;

        static void httpThreadFunc() {
            var currIcon = defaultIcon;

            server = new HttpListener();
            server.Prefixes.Add("http://127.0.0.1:15341/");
            server.Start();

            for (;;) {
                var context = server.GetContext();
                var response = context.Response;
                response.Headers.Add("Access-Control-Allow-Origin", "*");

                var path = context.Request.Url.LocalPath;
                string reply = "";
                if (path != null && path.StartsWith("/set-status/")) {
                    var name = path.Substring("/set-status/".Length);
                    Icon newIcon;
                    if (icons.TryGetValue(name, out newIcon)) {
                        // OK!
                    } else if (name.EndsWith("+") && icons.TryGetValue(name.Substring(0, name.Length - 1), out newIcon)) {
                        // OK!
                    } else newIcon = defaultIcon;
                    if (newIcon != currIcon) {
                        currIcon = newIcon;
                        trayIcon.Icon = newIcon;
                    }
                    reply = "OK!";
                } else {
                    response.StatusCode = 404;
                }

                var buffer = Encoding.UTF8.GetBytes(reply);
                response.ContentLength64 = buffer.Length;
                var st = response.OutputStream;
                st.Write(buffer, 0, buffer.Length);
                context.Response.Close();
            }
        }
        
        static void onExit(object sender, EventArgs e) {
            server.Stop();
            httpThread.Abort();
            Application.Exit();
        }
        
        [STAThread]
		static void Main() {
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

            try {
                foreach (var file in (new DirectoryInfo("Icons")).EnumerateFiles("*.ico")) {
                    var name = Path.GetFileNameWithoutExtension(file.Name);
                    icons[name] = Icon.ExtractAssociatedIcon(file.FullName);
                }
                defaultIcon = icons["default"];
            } catch (Exception e) {
                MessageBox.Show("Failed to enumerate icons:\n" + e.ToString(), "Uh oh", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
			}

            trayIcon = new NotifyIcon();
            trayIcon.Text = "Tray Icon for Discord Web\nDouble-click to exit.";
            trayIcon.Icon = defaultIcon;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += new EventHandler(onExit);

            httpThread = new Thread(new ThreadStart(httpThreadFunc));
            httpThread.Start();

			Application.Run();
		}
	}
}
