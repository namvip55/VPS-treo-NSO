using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;

namespace JarManagerStandalone
{
    static class Program
    {
        // --- THÔNG SỐ CẤU HÌNH VPS (ĐƯỢC GÁN KHI ĐÓNG GÓI CHO KHÁCH) ---
        public const string VPS_HOST = "14.250.250.229";
        public const int VPS_PORT = 12724;
        public const string VPS_USER = "root";
        public const string GAME_TITLE = "NSO GEMINI";
        public const string TARGET_ACCOUNT = "acc1";

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        private Process tunnelProcess = null;
        private Process activeVncProcess = null;
        private readonly object tunnelLock = new object();

        private string appDataDir;
        private string vncExePath;
        private string keyFilePath;
        private int currentLocalPort = 0;

        private Label lblStatus;
        private Button btnOpenGame;
        private Button btnRestart;
        private Button btnStop;

        public MainForm()
        {
            InitializeData();
            InitializeUI();
            CheckVpsOnlineAsync();
        }

        private void InitializeData()
        {
            // Thư mục lưu cache phụ trợ
            appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JarManager");
            if (!Directory.Exists(appDataDir))
            {
                Directory.CreateDirectory(appDataDir);
            }

            vncExePath = Path.Combine(appDataDir, "vncviewer.exe");

            // Đặt tên key file theo Host và Port để tránh xung đột giữa nhiều máy chủ khác nhau
            string safeHost = Program.VPS_HOST.Replace(".", "_").Replace(":", "_");
            string keyFileName = string.Format("client_{0}_{1}.key", safeHost, Program.VPS_PORT);
            keyFilePath = Path.Combine(appDataDir, keyFileName);

            // Trích xuất tài nguyên
            ExtractResource("VncViewer", vncExePath, false);
            ExtractResource("PrivateKey", keyFilePath, true);
        }

        private void ExtractResource(string resourceName, string outputPath, bool overwriteIfEmbedded = false)
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        // Nếu tài nguyên được nhúng trong file thực thi
                        if (!overwriteIfEmbedded && File.Exists(outputPath) && new FileInfo(outputPath).Length > 0)
                        {
                            if (resourceName == "PrivateKey") SecureKeyPermissions(outputPath);
                            return;
                        }
                        using (FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                        {
                            stream.CopyTo(fs);
                        }
                        if (resourceName == "PrivateKey") SecureKeyPermissions(outputPath);
                        return;
                    }
                }

                // Chế độ Portable (không nhúng resource): tìm kiếm file bên ngoài cạnh file EXE
                if (File.Exists(outputPath) && new FileInfo(outputPath).Length > 0)
                {
                    if (resourceName == "PrivateKey") SecureKeyPermissions(outputPath);
                    return;
                }

                string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(outputPath));
                // CHỈ fallback sang client.key nếu đang tìm PrivateKey, tuyệt đối không copy đè vào vncviewer.exe
                if (!File.Exists(localPath) && resourceName == "PrivateKey")
                {
                    localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "client.key");
                }

                if (File.Exists(localPath))
                {
                    File.Copy(localPath, outputPath, true);
                    if (resourceName == "PrivateKey") SecureKeyPermissions(outputPath);
                }
            }
            catch { }
        }

        private void SecureKeyPermissions(string path)
        {
            try
            {
                if (!File.Exists(path)) return;

                // 1. Phân quyền chuẩn bằng .NET FileSecurity (chỉ cho phép Current User toàn quyền)
                try
                {
                    FileInfo fi = new FileInfo(path);
                    FileSecurity fs = fi.GetAccessControl();
                    fs.SetAccessRuleProtection(true, false); // Tắt kế thừa từ AppData
                    SecurityIdentifier userSid = WindowsIdentity.GetCurrent().User;
                    fs.AddAccessRule(new FileSystemAccessRule(userSid, FileSystemRights.FullControl, AccessControlType.Allow));
                    fi.SetAccessControl(fs);
                    return;
                }
                catch { }

                // 2. Dự phòng bằng icacls của Windows - Sử dụng Environment.UserName thay vì %USERNAME% vì CreateProcess không expand biến môi trường
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "icacls.exe",
                    Arguments = string.Format("\"{0}\" /inheritance:r /grant:r \"{1}:(R,W)\"", path, Environment.UserName),
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using (Process p = Process.Start(psi))
                {
                    p.WaitForExit(3000);
                }
            }
            catch { }
        }

        private void CheckVpsOnlineAsync()
        {
            ThreadPool.QueueUserWorkItem(state =>
            {
                bool online = false;
                try
                {
                    using (TcpClient client = new TcpClient())
                    {
                        IAsyncResult ar = client.BeginConnect(Program.VPS_HOST, Program.VPS_PORT, null, null);
                        bool success = ar.AsyncWaitHandle.WaitOne(3000);
                        if (success)
                        {
                            client.EndConnect(ar);
                            online = true;
                        }
                    }
                }
                catch { }

                if (!this.IsDisposed && this.IsHandleCreated)
                {
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        if (online)
                        {
                            lblStatus.Text = "[●] CLOUD VPS: ONLINE";
                            lblStatus.ForeColor = Color.FromArgb(22, 163, 74);
                        }
                        else
                        {
                            lblStatus.Text = "[○] CLOUD VPS: OFFLINE (KHONG THE KET NOI)";
                            lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
                        }
                    });
                }
            });
        }

        private void InitializeUI()
        {
            this.Text = "JarManager - Treo Game " + Program.GAME_TITLE + " 24/7";
            this.Size = new Size(460, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(246, 248, 251);
            this.Font = new Font("Segoe UI", 9.5f);

            // --- HEADER ---
            Panel pnlHeader = new Panel
            {
                Location = new Point(15, 12),
                Size = new Size(415, 60),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(pnlHeader);

            lblStatus = new Label
            {
                Text = "[○] CLOUD VPS: DANG KIEM TRA...",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(12, 8),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblStatus);

            Label lblSub = new Label
            {
                Text = Program.VPS_HOST + ":" + Program.VPS_PORT + " | " + Program.GAME_TITLE + " [" + Program.TARGET_ACCOUNT + "]",
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(13, 31),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblSub);

            // --- NÚT VÀO GAME LỚN DUY NHẤT ---
            btnOpenGame = new Button
            {
                Text = "▶ VAO THAO TAC GAME (1 CLICK)",
                Location = new Point(15, 85),
                Size = new Size(415, 60),
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat
            };
            btnOpenGame.FlatAppearance.BorderSize = 0;
            btnOpenGame.Click += (s, e) => OpenGameScreen();
            this.Controls.Add(btnOpenGame);

            // --- KHỐI ĐIỀU KHIỂN PHỤ (Khởi động lại / Tắt) ---
            Panel pnlControls = new Panel
            {
                Location = new Point(15, 155),
                Size = new Size(415, 42)
            };
            this.Controls.Add(pnlControls);

            btnRestart = new Button
            {
                Text = "↺ Khoi Dong Lai Game",
                Location = new Point(0, 0),
                Size = new Size(200, 38),
                BackColor = Color.FromArgb(235, 240, 245),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat
            };
            btnRestart.FlatAppearance.BorderSize = 0;
            btnRestart.Click += (s, e) => RestartGameAsync();
            pnlControls.Controls.Add(btnRestart);

            btnStop = new Button
            {
                Text = "■ Tat Game",
                Location = new Point(215, 0),
                Size = new Size(200, 38),
                BackColor = Color.FromArgb(254, 226, 226),
                ForeColor = Color.FromArgb(185, 28, 28),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat
            };
            btnStop.FlatAppearance.BorderSize = 0;
            btnStop.Click += (s, e) => StopGameAsync();
            pnlControls.Controls.Add(btnStop);

            // --- KHUNG HƯỚNG DẪN BÀN PHÍM & TREO 24/7 ---
            Panel pnlHint = new Panel
            {
                Location = new Point(15, 208),
                Size = new Size(415, 135),
                BackColor = Color.FromArgb(240, 253, 244),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(pnlHint);

            Label lblHintTitle = new Label
            {
                Text = "• Huong Dan Dieu Khien & Treo 24/7:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(21, 128, 61),
                Location = new Point(10, 8),
                AutoSize = true
            };
            pnlHint.Controls.Add(lblHintTitle);

            Label lblHintBody = new Label
            {
                Text = "• Ban phim so NumPad (Chuan 100%):\n" +
                       "  - Phim 8: Nhay Len | Phim 2: Cui / Di Xuong\n" +
                       "  - Phim 4: Sang Trai | Phim 6: Sang Phai\n" +
                       "  - Phim 5: Danh thuong / Chon | Phim 7, 9: Skill\n" +
                       "• Thao tac game xong, ban chi can bam [X] tat cua so game di.\n" +
                       "✔ Game tren VPS van tu dong cay 24/7 binh thuong!",
                Font = new Font("Segoe UI", 8.8f),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(10, 28),
                AutoSize = true
            };
            pnlHint.Controls.Add(lblHintBody);

            this.FormClosing += (s, e) =>
            {
                ThreadPool.QueueUserWorkItem(state =>
                {
                    ExecuteSshCommand(string.Format("/opt/jar-manager/scripts/mode-idle.sh {0}", Program.TARGET_ACCOUNT));
                });
                CloseTunnel();
            };
        }

        private void SetUiBusy(bool busy, string statusMessage = null)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke((MethodInvoker)delegate { SetUiBusy(busy, statusMessage); });
                return;
            }

            btnOpenGame.Enabled = !busy;
            btnRestart.Enabled = !busy;
            btnStop.Enabled = !busy;

            if (statusMessage != null)
            {
                lblStatus.Text = statusMessage;
            }
        }

        private void RestartGameAsync()
        {
            SetUiBusy(true, "[↺] DANG KHOI DONG LAI GAME...");
            ThreadPool.QueueUserWorkItem(state =>
            {
                try
                {
                    ExecuteSshCommand(string.Format("/opt/jar-manager/scripts/stop-acc.sh {0}", Program.TARGET_ACCOUNT));
                    Thread.Sleep(500);
                    ExecuteSshCommand(string.Format("/opt/jar-manager/scripts/start-acc.sh {0}", Program.TARGET_ACCOUNT));
                    Thread.Sleep(800);

                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        SetUiBusy(false, "[●] CLOUD VPS: ONLINE");
                        OpenGameScreen();
                    });
                }
                catch (Exception ex)
                {
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        SetUiBusy(false, "[●] CLOUD VPS: ONLINE");
                        MessageBox.Show("Loi khoi dong lai game: " + ex.Message, "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    });
                }
            });
        }

        private void StopGameAsync()
        {
            var confirm = MessageBox.Show("Ban co chac muon tat game tren VPS?", "Xac nhan", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            SetUiBusy(true, "[■] DANG DUNG GAME TREN VPS...");
            ThreadPool.QueueUserWorkItem(state =>
            {
                try
                {
                    ExecuteSshCommand(string.Format("/opt/jar-manager/scripts/stop-acc.sh {0}", Program.TARGET_ACCOUNT));
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        SetUiBusy(false, "[●] CLOUD VPS: ONLINE");
                        MessageBox.Show("Da dung game tren VPS!", "Thong bao", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    });
                }
                catch (Exception ex)
                {
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        SetUiBusy(false, "[●] CLOUD VPS: ONLINE");
                        MessageBox.Show("Loi khi dung game: " + ex.Message, "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    });
                }
            });
        }

        private bool IsPortFree(int port)
        {
            try
            {
                TcpListener listener = new TcpListener(System.Net.IPAddress.Loopback, port);
                listener.Start();
                listener.Stop();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private int GetAvailableLocalPort(int startPort)
        {
            for (int p = startPort; p < startPort + 50; p++)
            {
                if (IsPortFree(p)) return p;
            }
            return startPort;
        }

        private bool EnsureTunnel()
        {
            lock (tunnelLock)
            {
                // Nếu tunnel của phiên này đang chạy và port đang mở thì tái sử dụng
                if (tunnelProcess != null && !tunnelProcess.HasExited && currentLocalPort > 0 && !IsPortFree(currentLocalPort))
                {
                    return true;
                }

                currentLocalPort = GetAvailableLocalPort(5901);

                if (File.Exists(keyFilePath))
                {
                    SecureKeyPermissions(keyFilePath);
                }

                try
                {
                    string sshArgs;
                    if (File.Exists(keyFilePath) && new FileInfo(keyFilePath).Length > 0)
                    {
                        sshArgs = string.Format("-i \"{0}\" -p {1} -N -L {2}:localhost:5901 -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o ConnectTimeout=8 -o ServerAliveInterval=30 -o ServerAliveCountMax=3 {3}@{4}",
                            keyFilePath, Program.VPS_PORT, currentLocalPort, Program.VPS_USER, Program.VPS_HOST);
                    }
                    else
                    {
                        sshArgs = string.Format("-p {0} -N -L {1}:localhost:5901 -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o ConnectTimeout=8 -o ServerAliveInterval=30 -o ServerAliveCountMax=3 {2}@{3}",
                            Program.VPS_PORT, currentLocalPort, Program.VPS_USER, Program.VPS_HOST);
                    }

                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "ssh.exe",
                        Arguments = sshArgs,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardError = true
                    };
                    tunnelProcess = Process.Start(psi);

                    // Chờ chủ động cho đến khi port tunnel thực sự được lắng nghe (tối đa 6 giây)
                    for (int i = 0; i < 30; i++)
                    {
                        Thread.Sleep(200);
                        if (!IsPortFree(currentLocalPort))
                        {
                            return true;
                        }
                        if (tunnelProcess.HasExited)
                        {
                            string err = tunnelProcess.StandardError.ReadToEnd();
                            this.BeginInvoke((MethodInvoker)delegate
                            {
                                MessageBox.Show("Khong the ket noi den VPS qua SSH:\n" + err, "Loi Ket Noi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            });
                            return false;
                        }
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        MessageBox.Show("Khong the khoi tao SSH Tunnel: " + ex.Message, "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    });
                    return false;
                }
            }
        }

        private void OpenGameScreen()
        {
            // Tránh mở đè nhiều cửa sổ VNC trên cùng một tunnel gây xung đột
            if (activeVncProcess != null && !activeVncProcess.HasExited)
            {
                MessageBox.Show("Màn hình game hiện đang được mở trên máy tính của bạn!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SetUiBusy(true, "[▶] DANG KET NOI VAO GAME...");

            ThreadPool.QueueUserWorkItem(state =>
            {
                if (!EnsureTunnel())
                {
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        SetUiBusy(false, "[●] CLOUD VPS: ONLINE");
                    });
                    return;
                }

                // Kích hoạt Active Mode (tối đa độ nhạy, 60 FPS, focus cửa sổ game)
                ExecuteSshCommand(string.Format("/opt/jar-manager/scripts/mode-active.sh {0}", Program.TARGET_ACCOUNT));

                string vncPath = vncExePath;
                if (!File.Exists(vncPath))
                {
                    vncPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vncviewer.exe");
                }

                if (!File.Exists(vncPath))
                {
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        SetUiBusy(false, "[●] CLOUD VPS: ONLINE");
                        MessageBox.Show("Khong tim thay vncviewer.exe! Vui long kiem tra file di kem.", "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    });
                    return;
                }

                try
                {
                    // Tối ưu độ mượt 60 FPS: -NoJPEG=1 loại bỏ nén ảnh mờ, tăng tốc CPU, nét từng pixel
                    string vncArgs = string.Format("localhost:{0} -PreferredEncoding=Tight -NoJPEG=1 -CompressLevel=1 -AutoSelect=0 -CustomCompressLevel=1", currentLocalPort);
                    activeVncProcess = Process.Start(vncPath, vncArgs);

                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        SetUiBusy(false, "[●] CLOUD VPS: ONLINE (DANG XEM)");
                    });

                    // Theo dõi tiến trình VNC trong background
                    if (activeVncProcess != null)
                    {
                        activeVncProcess.WaitForExit();
                        activeVncProcess = null;

                        // Khách đã đóng màn hình game -> chuyển về chế độ treo tiết kiệm 24/7 & đóng tunnel
                        ExecuteSshCommand(string.Format("/opt/jar-manager/scripts/mode-idle.sh {0}", Program.TARGET_ACCOUNT));
                        CloseTunnel();

                        this.BeginInvoke((MethodInvoker)delegate
                        {
                            if (!this.IsDisposed)
                            {
                                lblStatus.Text = "[●] CLOUD VPS: ONLINE";
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        SetUiBusy(false, "[●] CLOUD VPS: ONLINE");
                        MessageBox.Show("Khong the mo vncviewer: " + ex.Message, "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    });
                }
            });
        }

        private void ExecuteSshCommand(string cmd)
        {
            try
            {
                if (File.Exists(keyFilePath))
                {
                    SecureKeyPermissions(keyFilePath);
                }
                string sshArgs;
                if (File.Exists(keyFilePath) && new FileInfo(keyFilePath).Length > 0)
                {
                    sshArgs = string.Format("-i \"{0}\" -p {1} -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o ConnectTimeout=8 {2}@{3} \"{4}\"",
                        keyFilePath, Program.VPS_PORT, Program.VPS_USER, Program.VPS_HOST, cmd);
                }
                else
                {
                    sshArgs = string.Format("-p {0} -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o ConnectTimeout=8 {1}@{2} \"{3}\"",
                        Program.VPS_PORT, Program.VPS_USER, Program.VPS_HOST, cmd);
                }

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "ssh.exe",
                    Arguments = sshArgs,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using (Process p = Process.Start(psi))
                {
                    p.WaitForExit(6000);
                }
            }
            catch { }
        }

        private void CloseTunnel()
        {
            lock (tunnelLock)
            {
                try
                {
                    if (tunnelProcess != null && !tunnelProcess.HasExited)
                    {
                        tunnelProcess.Kill();
                    }
                }
                catch { }
                tunnelProcess = null;
                currentLocalPort = 0;
            }
        }
    }
}
