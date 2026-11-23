using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
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
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private const int SW_RESTORE = 9;

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
            this.Shown += (s, e) => CheckVpsOnlineAsync();
        }

        private void InitializeData()
        {
            appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JarManager");
            if (!Directory.Exists(appDataDir))
            {
                Directory.CreateDirectory(appDataDir);
            }

            vncExePath = Path.Combine(appDataDir, "vncviewer.exe");

            string safeHost = Program.VPS_HOST.Replace(".", "_").Replace(":", "_");
            string keyFileName = string.Format("client_{0}_{1}.key", safeHost, Program.VPS_PORT);
            keyFilePath = Path.Combine(appDataDir, keyFileName);

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

                if (File.Exists(outputPath) && new FileInfo(outputPath).Length > 0)
                {
                    if (resourceName == "PrivateKey") SecureKeyPermissions(outputPath);
                    return;
                }

                string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(outputPath));
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

                try
                {
                    FileInfo fi = new FileInfo(path);
                    FileSecurity fs = fi.GetAccessControl();
                    fs.SetAccessRuleProtection(true, false);
                    SecurityIdentifier userSid = WindowsIdentity.GetCurrent().User;
                    fs.AddAccessRule(new FileSystemAccessRule(userSid, FileSystemRights.FullControl, AccessControlType.Allow));
                    fi.SetAccessControl(fs);
                    return;
                }
                catch { }

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

        private void SafeInvoke(Action action)
        {
            if (this.IsDisposed || !this.IsHandleCreated) return;
            try
            {
                if (this.InvokeRequired)
                {
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        if (!this.IsDisposed && this.IsHandleCreated)
                        {
                            action();
                        }
                    });
                }
                else
                {
                    action();
                }
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
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

                SafeInvoke(() =>
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

            // --- NÚT VÀO GAME LỚN DUY NHẤT (Có hiệu ứng hover, click down rõ rệt) ---
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
            btnOpenGame.FlatAppearance.MouseOverBackColor = Color.FromArgb(34, 197, 94);
            btnOpenGame.FlatAppearance.MouseDownBackColor = Color.FromArgb(21, 128, 61);
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
                ForeColor = Color.FromArgb(30, 41, 59),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat
            };
            btnRestart.FlatAppearance.BorderSize = 0;
            btnRestart.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 230, 242);
            btnRestart.FlatAppearance.MouseDownBackColor = Color.FromArgb(195, 210, 230);
            btnRestart.Click += (s, e) => RestartGameAsync();
            pnlControls.Controls.Add(btnRestart);

            btnStop = new Button
            {
                Text = "■ Tat Game",
                Location = new Point(215, 0),
                Size = new Size(200, 38),
                BackColor = Color.FromArgb(254, 226, 226),
                ForeColor = Color.FromArgb(185, 28, 28),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat
            };
            btnStop.FlatAppearance.BorderSize = 0;
            btnStop.FlatAppearance.MouseOverBackColor = Color.FromArgb(254, 202, 202);
            btnStop.FlatAppearance.MouseDownBackColor = Color.FromArgb(248, 113, 113);
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
                try
                {
                    FireAndForgetSshCommand(string.Format("/opt/jar-manager/scripts/mode-idle.sh {0}", Program.TARGET_ACCOUNT));
                }
                catch { }
                CloseTunnel();
            };
        }

        // --- CÁC HÀM CẬP NHẬT TRẠNG THÁI GIAO DIỆN TRỰC QUAN (CHỐNG DOUBLE-CLICK) ---

        private void SetConnectingState()
        {
            // Phản hồi lập tức 0ms ngay khi click chuột
            btnOpenGame.Enabled = false;
            btnOpenGame.Text = "⏳ ĐANG KẾT NỐI VÀO GAME... (VUI LÒNG CHỜ)";
            btnOpenGame.BackColor = Color.FromArgb(217, 119, 6); // Màu vàng cam ấm báo hiệu đang xử lý
            btnOpenGame.Cursor = Cursors.WaitCursor;

            btnRestart.Enabled = false;
            btnStop.Enabled = false;

            lblStatus.Text = "[⏳] DANG KHOI TAO SSH TUNNEL & VAO GAME...";
            lblStatus.ForeColor = Color.FromArgb(217, 119, 6);
        }

        private void SetActiveGameState()
        {
            // Khi màn hình VNC đã mở: Nút chuyển sang trạng thái Active màu xanh dương
            btnOpenGame.Enabled = true;
            btnOpenGame.Text = "✔ CỬA SỔ GAME ĐANG MỞ (BẤM ĐỂ HIỆN LÊN ĐẦU)";
            btnOpenGame.BackColor = Color.FromArgb(37, 99, 235); // Royal Blue #2563eb
            btnOpenGame.FlatAppearance.MouseOverBackColor = Color.FromArgb(59, 130, 246);
            btnOpenGame.FlatAppearance.MouseDownBackColor = Color.FromArgb(29, 78, 216);
            btnOpenGame.Cursor = Cursors.Hand;

            btnRestart.Enabled = true;
            btnStop.Enabled = true;

            lblStatus.Text = "[●] CLOUD VPS: ONLINE (DANG XEM GAME 60 FPS)";
            lblStatus.ForeColor = Color.FromArgb(22, 163, 74);
        }

        private void ResetToIdleState()
        {
            // Trở về trạng thái ban đầu khi không có cửa sổ game nào đang mở
            btnOpenGame.Enabled = true;
            btnOpenGame.Text = "▶ VAO THAO TAC GAME (1 CLICK)";
            btnOpenGame.BackColor = Color.FromArgb(22, 163, 74);
            btnOpenGame.FlatAppearance.MouseOverBackColor = Color.FromArgb(34, 197, 94);
            btnOpenGame.FlatAppearance.MouseDownBackColor = Color.FromArgb(21, 128, 61);
            btnOpenGame.Cursor = Cursors.Hand;

            btnRestart.Enabled = true;
            btnRestart.Text = "↺ Khoi Dong Lai Game";
            btnRestart.BackColor = Color.FromArgb(235, 240, 245);

            btnStop.Enabled = true;
            btnStop.Text = "■ Tat Game";
            btnStop.BackColor = Color.FromArgb(254, 226, 226);

            lblStatus.Text = "[●] CLOUD VPS: ONLINE";
            lblStatus.ForeColor = Color.FromArgb(22, 163, 74);
        }

        private void BringVncToFront()
        {
            try
            {
                if (activeVncProcess != null && !activeVncProcess.HasExited)
                {
                    IntPtr hWnd = activeVncProcess.MainWindowHandle;
                    if (hWnd != IntPtr.Zero)
                    {
                        ShowWindow(hWnd, SW_RESTORE);
                        SetForegroundWindow(hWnd);
                    }
                }
            }
            catch { }
        }

        private void RestartGameAsync()
        {
            btnRestart.Enabled = false;
            btnRestart.Text = "⏳ Đang khởi động...";
            btnRestart.BackColor = Color.FromArgb(254, 240, 138);
            btnOpenGame.Enabled = false;
            btnStop.Enabled = false;

            lblStatus.Text = "[↺] DANG KHOI DONG LAI GAME...";
            lblStatus.ForeColor = Color.FromArgb(217, 119, 6);

            ThreadPool.QueueUserWorkItem(state =>
            {
                try
                {
                    ExecuteSshCommand(string.Format("/opt/jar-manager/scripts/stop-acc.sh {0}", Program.TARGET_ACCOUNT));
                    Thread.Sleep(500);
                    ExecuteSshCommand(string.Format("/opt/jar-manager/scripts/start-acc.sh {0}", Program.TARGET_ACCOUNT));
                    Thread.Sleep(800);

                    SafeInvoke(() =>
                    {
                        ResetToIdleState();
                        OpenGameScreen();
                    });
                }
                catch (Exception ex)
                {
                    SafeInvoke(() =>
                    {
                        ResetToIdleState();
                        MessageBox.Show("Loi khoi dong lai game: " + ex.Message, "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    });
                }
            });
        }

        private void StopGameAsync()
        {
            var confirm = MessageBox.Show("Ban co chac muon tat game tren VPS?", "Xac nhan", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            btnStop.Enabled = false;
            btnStop.Text = "⏳ Đang tắt game...";
            btnStop.BackColor = Color.FromArgb(254, 202, 202);
            btnOpenGame.Enabled = false;
            btnRestart.Enabled = false;

            lblStatus.Text = "[■] DANG DUNG GAME TREN VPS...";
            lblStatus.ForeColor = Color.FromArgb(220, 38, 38);

            ThreadPool.QueueUserWorkItem(state =>
            {
                try
                {
                    ExecuteSshCommand(string.Format("/opt/jar-manager/scripts/stop-acc.sh {0}", Program.TARGET_ACCOUNT));
                    SafeInvoke(() =>
                    {
                        ResetToIdleState();
                        lblStatus.Text = "[●] CLOUD VPS: ONLINE (DA DUNG GAME)";
                        MessageBox.Show("Da dung game tren VPS!", "Thong bao", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    });
                }
                catch (Exception ex)
                {
                    SafeInvoke(() =>
                    {
                        ResetToIdleState();
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
                            SafeInvoke(() =>
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
                    SafeInvoke(() =>
                    {
                        MessageBox.Show("Khong the khoi tao SSH Tunnel: " + ex.Message, "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    });
                    return false;
                }
            }
        }

        private void OpenGameScreen()
        {
            // Nếu cửa sổ VNC đang mở: Đưa ngay cửa sổ lên đầu màn hình thay vì báo lỗi
            if (activeVncProcess != null && !activeVncProcess.HasExited)
            {
                BringVncToFront();
                return;
            }

            // PHẢN HỒI THỊ GIÁC LẬP TỨC 0MS: Đổi màu, đổi text, khóa nút chống double-click
            SetConnectingState();

            ThreadPool.QueueUserWorkItem(state =>
            {
                if (!EnsureTunnel())
                {
                    SafeInvoke(() =>
                    {
                        ResetToIdleState();
                    });
                    return;
                }

                ExecuteSshCommand(string.Format("/opt/jar-manager/scripts/mode-active.sh {0}", Program.TARGET_ACCOUNT));

                string vncPath = vncExePath;
                if (!File.Exists(vncPath))
                {
                    vncPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vncviewer.exe");
                }

                if (!File.Exists(vncPath))
                {
                    SafeInvoke(() =>
                    {
                        ResetToIdleState();
                        MessageBox.Show("Khong tim thay vncviewer.exe! Vui long kiem tra file di kem.", "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    });
                    return;
                }

                try
                {
                    string vncArgs = string.Format("localhost:{0} -PreferredEncoding=Tight -NoJPEG=1 -CompressLevel=1 -AutoSelect=0 -CustomCompressLevel=1", currentLocalPort);
                    activeVncProcess = Process.Start(vncPath, vncArgs);

                    SafeInvoke(() =>
                    {
                        SetActiveGameState();
                    });

                    if (activeVncProcess != null)
                    {
                        activeVncProcess.WaitForExit();
                        activeVncProcess = null;

                        ExecuteSshCommand(string.Format("/opt/jar-manager/scripts/mode-idle.sh {0}", Program.TARGET_ACCOUNT));
                        CloseTunnel();

                        SafeInvoke(() =>
                        {
                            ResetToIdleState();
                        });
                    }
                }
                catch (Exception ex)
                {
                    SafeInvoke(() =>
                    {
                        ResetToIdleState();
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

        private void FireAndForgetSshCommand(string cmd)
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
                    sshArgs = string.Format("-i \"{0}\" -p {1} -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o ConnectTimeout=5 {2}@{3} \"{4}\"",
                        keyFilePath, Program.VPS_PORT, Program.VPS_USER, Program.VPS_HOST, cmd);
                }
                else
                {
                    sshArgs = string.Format("-p {0} -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o ConnectTimeout=5 {1}@{2} \"{3}\"",
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
                Process.Start(psi);
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
