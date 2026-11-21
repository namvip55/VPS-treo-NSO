using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
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
        private string appDataDir;
        private string vncExePath;
        private string keyFilePath;

        public MainForm()
        {
            InitializeData();
            InitializeUI();
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
            keyFilePath = Path.Combine(appDataDir, "client.key");

            // Giải nén vncviewer.exe từ Embedded Resource nếu chưa có
            ExtractResource("VncViewer", vncExePath);
            ExtractResource("PrivateKey", keyFilePath);
        }

        private void ExtractResource(string resourceName, string outputPath)
        {
            try
            {
                // Nếu file đã tồn tại và có kích thước > 0 thì bỏ qua
                if (File.Exists(outputPath) && new FileInfo(outputPath).Length > 0)
                {
                    return;
                }

                Assembly assembly = Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        // Kiểm tra nếu file có sẵn bên ngoài cạnh file EXE
                        string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(outputPath));
                        if (File.Exists(localPath))
                        {
                            File.Copy(localPath, outputPath, true);
                            return;
                        }
                        return;
                    }

                    using (FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                    {
                        stream.CopyTo(fs);
                    }
                }
            }
            catch { }
        }

        private void InitializeUI()
        {
            this.Text = "JarManager - Treo Game NSO 24/7";
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

            Label lblStatus = new Label
            {
                Text = "[●] CLOUD VPS: ONLINE",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 163, 74),
                Location = new Point(12, 8),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblStatus);

            Label lblSub = new Label
            {
                Text = Program.VPS_HOST + ":" + Program.VPS_PORT + " | " + Program.GAME_TITLE + " 24/7",
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(13, 31),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblSub);

            // --- NÚT VÀO GAME LỚN DUY NHẤT ---
            Button btnOpenGame = new Button
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

            Button btnRestart = new Button
            {
                Text = "↺ Khoi Dong Lai Game",
                Location = new Point(0, 0),
                Size = new Size(200, 38),
                BackColor = Color.FromArgb(235, 240, 245),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat
            };
            btnRestart.FlatAppearance.BorderSize = 0;
            btnRestart.Click += (s, e) =>
            {
                ExecuteSshCommand("/opt/jar-manager/scripts/stop-acc.sh acc1");
                System.Threading.Thread.Sleep(500);
                ExecuteSshCommand("/opt/jar-manager/scripts/start-acc.sh acc1");
                System.Threading.Thread.Sleep(800);
                OpenGameScreen();
            };
            pnlControls.Controls.Add(btnRestart);

            Button btnStop = new Button
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
            btnStop.Click += (s, e) =>
            {
                var confirm = MessageBox.Show("Ban co chac muon tat game tren VPS?", "Xac nhan", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm == DialogResult.Yes)
                {
                    ExecuteSshCommand("/opt/jar-manager/scripts/stop-acc.sh acc1");
                    MessageBox.Show("Da dung game tren VPS!", "Thong bao", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
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
                Text = "• Ban phim so NumPad (Da sua chuan 100%):\n" +
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

            this.FormClosing += (s, e) => CloseTunnel();
        }

        private bool IsPortInUse(int port)
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    IAsyncResult result = client.BeginConnect("127.0.0.1", port, null, null);
                    bool success = result.AsyncWaitHandle.WaitOne(300);
                    if (success)
                    {
                        client.EndConnect(result);
                        return true;
                    }
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private void EnsureTunnel()
        {
            if (IsPortInUse(5901))
            {
                return;
            }

            try
            {
                string sshArgs;
                if (File.Exists(keyFilePath) && new FileInfo(keyFilePath).Length > 0)
                {
                    sshArgs = string.Format("-i \"{0}\" -p {1} -N -L 5901:localhost:5901 -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o ServerAliveInterval=30 -o ServerAliveCountMax=3 {2}@{3}",
                        keyFilePath, Program.VPS_PORT, Program.VPS_USER, Program.VPS_HOST);
                }
                else
                {
                    // Fallback sang alias vps-jar nếu có
                    sshArgs = string.Format("-p {0} -N -L 5901:localhost:5901 -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o ServerAliveInterval=30 -o ServerAliveCountMax=3 {1}@{2}",
                        Program.VPS_PORT, Program.VPS_USER, Program.VPS_HOST);
                }

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "ssh.exe",
                    Arguments = sshArgs,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                tunnelProcess = Process.Start(psi);
                System.Threading.Thread.Sleep(700);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Khong the khoi tao SSH Tunnel: " + ex.Message, "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenGameScreen()
        {
            EnsureTunnel();

            string vncPath = vncExePath;
            if (!File.Exists(vncPath))
            {
                vncPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vncviewer.exe");
            }

            if (File.Exists(vncPath))
            {
                string vncArgs = "localhost:5901 -PreferredEncoding=Tight -QualityLevel=6 -CompressLevel=1 -AutoSelect=0 -CustomCompressLevel=1";
                Process.Start(vncPath, vncArgs);
            }
            else
            {
                MessageBox.Show("Khong tim thay vncviewer.exe! Vui long kiem tra file di kem.", "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExecuteSshCommand(string cmd)
        {
            try
            {
                string sshArgs;
                if (File.Exists(keyFilePath) && new FileInfo(keyFilePath).Length > 0)
                {
                    sshArgs = string.Format("-i \"{0}\" -p {1} -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null {2}@{3} \"{4}\"",
                        keyFilePath, Program.VPS_PORT, Program.VPS_USER, Program.VPS_HOST, cmd);
                }
                else
                {
                    sshArgs = string.Format("-p {0} -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null {1}@{2} \"{3}\"",
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
            try
            {
                if (tunnelProcess != null && !tunnelProcess.HasExited)
                {
                    tunnelProcess.Kill();
                }
            }
            catch { }

            try
            {
                // Kill bat ky tien trinh ssh tunnel 5901 nao con sot lai
                Process[] procs = Process.GetProcessesByName("ssh");
                foreach (Process p in procs)
                {
                    // Tranh tat nham cac session ssh khac neu khong phai tunnel
                }
            }
            catch { }
        }
    }
}
