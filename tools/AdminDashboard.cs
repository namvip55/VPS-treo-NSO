using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace JarManagerAdmin
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new DashboardForm());
        }
    }

    public class VpsInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string User { get; set; }
        public string Key { get; set; }
        public string Game { get; set; }
        public string GameTitle { get; set; }
        public string WebUrl { get; set; }
        public string DefaultPin { get; set; }
        public bool IsOnline { get; set; }
        public int PingMs { get; set; }
    }

    public class SlotInfo
    {
        public int Slot { get; set; }
        public string Game { get; set; }
        public string Acc { get; set; }
        public string Pin { get; set; }
        public int Port { get; set; }
        public string Status { get; set; }
        public string Pid { get; set; }
    }

    public class DashboardForm : Form
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private string projectDir;
        private string registryPath;
        private Dictionary<string, VpsInfo> servers = new Dictionary<string, VpsInfo>();
        private VpsInfo currentVps = null;

        // UI Controls
        private ListBox lstVps;
        private Label lblHeaderTitle;
        private Label lblHeaderSub;
        private Label lblStatusBadge;
        private Button btnRefreshAll;
        private Button btnAddVps;
        private Button btnCopyIphoneLink;
        private Button btnExportExe;

        // Hardware Controls
        private Label lblRamStats;
        private ProgressBar prgRam;
        private Label lblSwapStats;
        private Label lblCpuStats;
        private Label lblCapacityHint;

        // Slot Controls
        private ListView lvwSlots;
        private Button btnViewGame;
        private Button btnRestartSlot;
        private Button btnDeleteSlot;
        private Button btnAddSlot;

        private Process activeTunnel = null;

        public DashboardForm()
        {
            InitializeData();
            InitializeUI();
            this.Shown += (s, e) => LoadRegistryAndCheckAll();
        }

        private void InitializeData()
        {
            projectDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            registryPath = Path.Combine(projectDir, "vps_registry.json");
            if (!File.Exists(registryPath))
            {
                string parent = Path.GetFullPath(Path.Combine(projectDir, ".."));
                if (File.Exists(Path.Combine(parent, "vps_registry.json")))
                {
                    projectDir = parent;
                    registryPath = Path.Combine(projectDir, "vps_registry.json");
                }
            }
        }

        private void SafeInvoke(Action action)
        {
            try
            {
                if (this.IsDisposed || !this.IsHandleCreated) return;
                if (this.InvokeRequired) this.BeginInvoke(action);
                else action();
            }
            catch { }
        }

        private void InitializeUI()
        {
            this.Text = "JarManager Central Admin - Giám Sát VPS & Quản Lý Slot Game";
            this.Size = new Size(980, 640);
            this.MinimumSize = new Size(920, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(243, 246, 250);
            this.Font = new Font("Segoe UI", 9.5f);

            // --- MAIN LAYOUT: LEFT SIDEBAR + RIGHT CONTENT ---
            Panel pnlSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 280,
                BackColor = Color.White,
                Padding = new Padding(12)
            };
            this.Controls.Add(pnlSidebar);

            Panel pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 12, 16, 16),
                AutoScroll = true
            };
            this.Controls.Add(pnlContent);

            // --- SIDEBAR CONTENT ---
            Label lblSideTitle = new Label
            {
                Text = "🖥️ DANH SÁCH VPS",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(12, 12),
                AutoSize = true
            };
            pnlSidebar.Controls.Add(lblSideTitle);

            btnRefreshAll = new Button
            {
                Text = "🔄 Làm Mới Toàn Bộ",
                Location = new Point(12, 40),
                Size = new Size(256, 32),
                BackColor = Color.FromArgb(238, 242, 246),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRefreshAll.FlatAppearance.BorderSize = 0;
            btnRefreshAll.Click += (s, e) => LoadRegistryAndCheckAll();
            pnlSidebar.Controls.Add(btnRefreshAll);

            lstVps = new ListBox
            {
                Location = new Point(12, 80),
                Size = new Size(256, 460),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10f),
                ItemHeight = 28,
                DrawMode = DrawMode.OwnerDrawFixed
            };
            lstVps.DrawItem += LstVps_DrawItem;
            lstVps.SelectedIndexChanged += (s, e) => OnSelectedVpsChanged();
            pnlSidebar.Controls.Add(lstVps);

            btnAddVps = new Button
            {
                Text = "+ Thêm VPS Mới",
                Location = new Point(12, 550),
                Size = new Size(256, 36),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAddVps.FlatAppearance.BorderSize = 0;
            btnAddVps.Click += (s, e) => ShowAddVpsDialog();
            pnlSidebar.Controls.Add(btnAddVps);

            // --- CONTENT: TOP HEADER CARD ---
            Panel pnlHeader = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(660, 80),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlContent.Controls.Add(pnlHeader);

            lblHeaderTitle = new Label
            {
                Text = "Đang chọn VPS...",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(14, 10),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblHeaderTitle);

            lblHeaderSub = new Label
            {
                Text = "Host: - | Port: -",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(15, 36),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblHeaderSub);

            lblStatusBadge = new Label
            {
                Text = "[○] ĐANG KIỂM TRA...",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(15, 56),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblStatusBadge);

            btnCopyIphoneLink = new Button
            {
                Text = "📱 Link Web iPhone",
                Location = new Point(400, 12),
                Size = new Size(125, 32),
                BackColor = Color.FromArgb(240, 253, 244),
                ForeColor = Color.FromArgb(21, 128, 61),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCopyIphoneLink.FlatAppearance.BorderColor = Color.FromArgb(187, 247, 208);
            btnCopyIphoneLink.Click += (s, e) => CopyIphoneLink();
            pnlHeader.Controls.Add(btnCopyIphoneLink);

            btnExportExe = new Button
            {
                Text = "📦 Xuất File EXE",
                Location = new Point(530, 12),
                Size = new Size(115, 32),
                BackColor = Color.FromArgb(238, 242, 246),
                ForeColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnExportExe.FlatAppearance.BorderSize = 0;
            btnExportExe.Click += (s, e) => ExportExeForClient();
            pnlHeader.Controls.Add(btnExportExe);

            // --- CONTENT: HARDWARE METRICS CARD ---
            GroupBox grpSpecs = new GroupBox
            {
                Text = "📊 THÔNG SỐ PHẦN CỨNG & TÀI NGUYÊN MÁY CHỦ",
                Location = new Point(0, 92),
                Size = new Size(660, 140),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                BackColor = Color.White
            };
            pnlContent.Controls.Add(grpSpecs);

            lblRamStats = new Label
            {
                Text = "RAM: Đang đo...",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(51, 65, 85),
                Location = new Point(16, 25),
                AutoSize = true
            };
            grpSpecs.Controls.Add(lblRamStats);

            prgRam = new ProgressBar
            {
                Location = new Point(18, 48),
                Size = new Size(620, 14),
                Value = 0,
                Maximum = 100
            };
            grpSpecs.Controls.Add(prgRam);

            lblSwapStats = new Label
            {
                Text = "Swap: -",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(16, 68),
                AutoSize = true
            };
            grpSpecs.Controls.Add(lblSwapStats);

            lblCpuStats = new Label
            {
                Text = "CPU Load: -",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(320, 68),
                AutoSize = true
            };
            grpSpecs.Controls.Add(lblCpuStats);

            lblCapacityHint = new Label
            {
                Text = "💡 Đánh giá sức chứa: Đang phân tích...",
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                ForeColor = Color.FromArgb(21, 128, 61),
                Location = new Point(16, 100),
                AutoSize = true
            };
            grpSpecs.Controls.Add(lblCapacityHint);

            // --- CONTENT: GAME SLOTS CARD ---
            GroupBox grpSlots = new GroupBox
            {
                Text = "🎮 CÁC TỰA GAME & SLOT ĐANG TREO TRÊN VPS NÀY",
                Location = new Point(0, 242),
                Size = new Size(660, 330),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                BackColor = Color.White
            };
            pnlContent.Controls.Add(grpSlots);

            lvwSlots = new ListView
            {
                Location = new Point(16, 26),
                Size = new Size(624, 220),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Font = new Font("Segoe UI", 9.5f),
                MultiSelect = false
            };
            lvwSlots.Columns.Add("SLOT", 65);
            lvwSlots.Columns.Add("TỰA GAME", 160);
            lvwSlots.Columns.Add("ACC / PROFILE", 95);
            lvwSlots.Columns.Add("MÃ PIN", 75);
            lvwSlots.Columns.Add("PORT VNC", 80);
            lvwSlots.Columns.Add("TRẠNG THÁI", 140);
            grpSlots.Controls.Add(lvwSlots);

            // Slot Action Buttons
            btnViewGame = new Button
            {
                Text = "▶ Xem Live Màn Hình",
                Location = new Point(16, 260),
                Size = new Size(160, 36),
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnViewGame.FlatAppearance.BorderSize = 0;
            btnViewGame.Click += (s, e) => ViewSelectedSlotGame();
            grpSlots.Controls.Add(btnViewGame);

            btnRestartSlot = new Button
            {
                Text = "↺ Khởi Động Lại",
                Location = new Point(184, 260),
                Size = new Size(125, 36),
                BackColor = Color.FromArgb(238, 242, 246),
                ForeColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRestartSlot.FlatAppearance.BorderSize = 0;
            btnRestartSlot.Click += (s, e) => RestartSelectedSlot();
            grpSlots.Controls.Add(btnRestartSlot);

            btnDeleteSlot = new Button
            {
                Text = "🗑 Xóa Slot Khách",
                Location = new Point(316, 260),
                Size = new Size(130, 36),
                BackColor = Color.FromArgb(254, 226, 226),
                ForeColor = Color.FromArgb(185, 28, 28),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDeleteSlot.FlatAppearance.BorderSize = 0;
            btnDeleteSlot.Click += (s, e) => RemoveSelectedSlot();
            grpSlots.Controls.Add(btnDeleteSlot);

            btnAddSlot = new Button
            {
                Text = "+ Thêm Slot Game Mới",
                Location = new Point(460, 260),
                Size = new Size(180, 36),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAddSlot.FlatAppearance.BorderSize = 0;
            btnAddSlot.Click += (s, e) => ShowAddSlotDialog();
            grpSlots.Controls.Add(btnAddSlot);

            this.FormClosing += (s, e) =>
            {
                if (activeTunnel != null && !activeTunnel.HasExited)
                {
                    try { activeTunnel.Kill(); } catch { }
                }
            };
        }

        private void LstVps_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= lstVps.Items.Count) return;
            string key = lstVps.Items[e.Index].ToString();
            VpsInfo info = servers.ContainsKey(key) ? servers[key] : null;

            e.DrawBackground();
            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            Color dotColor = info != null && info.IsOnline ? Color.FromArgb(34, 197, 94) : Color.FromArgb(239, 68, 68);
            using (SolidBrush dotBrush = new SolidBrush(dotColor))
            {
                e.Graphics.FillEllipse(dotBrush, e.Bounds.X + 8, e.Bounds.Y + 9, 10, 10);
            }

            string text = info != null ? string.Format("{0} ({1})", info.Name, info.Host) : key;
            Color textColor = isSelected ? Color.White : Color.FromArgb(30, 41, 59);
            using (SolidBrush textBrush = new SolidBrush(textColor))
            {
                e.Graphics.DrawString(text, e.Font, textBrush, e.Bounds.X + 24, e.Bounds.Y + 5);
            }
            e.DrawFocusRectangle();
        }

        private void LoadRegistryAndCheckAll()
        {
            servers.Clear();
            lstVps.Items.Clear();

            if (!File.Exists(registryPath))
            {
                MessageBox.Show("Không tìm thấy file vps_registry.json!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string json = File.ReadAllText(registryPath);
                JavaScriptSerializer js = new JavaScriptSerializer();
                Dictionary<string, object> dict = js.Deserialize<Dictionary<string, object>>(json);

                Dictionary<string, object> srvDict = null;
                if (dict.ContainsKey("servers"))
                {
                    srvDict = dict["servers"] as Dictionary<string, object>;
                }
                else
                {
                    srvDict = dict;
                }

                if (srvDict != null)
                {
                    foreach (var kvp in srvDict)
                    {
                        var s = kvp.Value as Dictionary<string, object>;
                        if (s == null) continue;

                        VpsInfo info = new VpsInfo
                        {
                            Id = kvp.Key,
                            Name = s.ContainsKey("name") ? s["name"].ToString() : kvp.Key,
                            Host = s.ContainsKey("host") ? s["host"].ToString() : "127.0.0.1",
                            Port = s.ContainsKey("port") ? Convert.ToInt32(s["port"]) : 22,
                            User = s.ContainsKey("user") ? s["user"].ToString() : "root",
                            Key = s.ContainsKey("key") ? s["key"].ToString() : "~/.ssh/id_ed25519_jar",
                            Game = s.ContainsKey("game") ? s["game"].ToString() : "game.jar",
                            GameTitle = s.ContainsKey("game_title") ? s["game_title"].ToString() : "NSO",
                            WebUrl = s.ContainsKey("web_url") ? s["web_url"].ToString() : (s.ContainsKey("ngrok_domain") ? "https://" + s["ngrok_domain"].ToString() : "https://mistily-stipular-kristopher.ngrok-free.dev"),
                            DefaultPin = s.ContainsKey("default_pin") ? s["default_pin"].ToString() : "1234",
                            IsOnline = false,
                            PingMs = 0
                        };
                        servers[info.Id] = info;
                        lstVps.Items.Add(info.Id);
                    }
                }

                if (lstVps.Items.Count > 0)
                {
                    lstVps.SelectedIndex = 0;
                }

                // Chạy ping & kiểm tra port ngầm
                ThreadPool.QueueUserWorkItem(state =>
                {
                    foreach (var info in servers.Values)
                    {
                        CheckSingleVpsPort(info);
                    }
                    SafeInvoke(() => lstVps.Invalidate());
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi đọc cấu hình: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CheckSingleVpsPort(VpsInfo info)
        {
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    IAsyncResult ar = client.BeginConnect(info.Host, info.Port, null, null);
                    bool ok = ar.AsyncWaitHandle.WaitOne(2500);
                    if (ok)
                    {
                        client.EndConnect(ar);
                        sw.Stop();
                        info.IsOnline = true;
                        info.PingMs = (int)sw.ElapsedMilliseconds;
                        return;
                    }
                }
            }
            catch { }
            info.IsOnline = false;
            info.PingMs = 0;
        }

        private void OnSelectedVpsChanged()
        {
            if (lstVps.SelectedItem == null) return;
            string key = lstVps.SelectedItem.ToString();
            if (!servers.ContainsKey(key)) return;

            currentVps = servers[key];
            lblHeaderTitle.Text = string.Format("{0} [{1}]", currentVps.Name, currentVps.Id);
            lblHeaderSub.Text = string.Format("Host: {0} | Port NAT: {1} | User: {2}", currentVps.Host, currentVps.Port, currentVps.User);

            if (currentVps.IsOnline)
            {
                lblStatusBadge.Text = string.Format("[●] ONLINE ({0} ms)", currentVps.PingMs);
                lblStatusBadge.ForeColor = Color.FromArgb(22, 163, 74);
            }
            else
            {
                lblStatusBadge.Text = "[○] OFFLINE (Chưa thể kết nối)";
                lblStatusBadge.ForeColor = Color.FromArgb(220, 38, 38);
            }

            // Xóa dữ liệu cũ
            lblRamStats.Text = "RAM: Đang kết nối SSH đọc thông số...";
            prgRam.Value = 0;
            lvwSlots.Items.Clear();

            // Nạp thông số chi tiết qua SSH ngầm
            ThreadPool.QueueUserWorkItem(state => FetchVpsDetailsAsync(currentVps));
        }

        private void FetchVpsDetailsAsync(VpsInfo vps)
        {
            // Lệnh kết hợp: Đọc RAM, Swap, Load, và slots.json
            string remoteCmd = "free -m; echo '---LOAD---'; cat /proc/loadavg 2>/dev/null; echo '---SLOTS---'; cat /opt/jar-manager/slots.json 2>/dev/null || echo '{}'; echo '---PROCESSES---'; ps -eo pid,cmd | grep java | grep -v grep";
            string output = ExecuteSsh(vps, remoteCmd);

            SafeInvoke(() =>
            {
                if (currentVps != vps) return; // Người dùng đã chuyển sang VPS khác
                ParseAndUpdateVpsStats(output);
            });
        }

        private void ParseAndUpdateVpsStats(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                lblRamStats.Text = "RAM: Không thể lấy dữ liệu (Mất kết nối SSH hoặc VPS tắt)";
                lblCapacityHint.Text = "💡 Đánh giá sức chứa: Vui lòng kiểm tra lại trạng thái VPS.";
                return;
            }

            try
            {
                // 1. Parse RAM & Swap
                Match mMem = Regex.Match(raw, @"Mem:\s+(\d+)\s+(\d+)\s+(\d+)\s+(\d+)\s+(\d+)\s+(\d+)");
                if (mMem.Success)
                {
                    int totalRam = int.Parse(mMem.Groups[1].Value);
                    int usedRam = int.Parse(mMem.Groups[2].Value);
                    int freeRam = int.Parse(mMem.Groups[3].Value);
                    int availRam = int.Parse(mMem.Groups[6].Value);

                    int pctUsed = (int)((double)(totalRam - availRam) / totalRam * 100);
                    if (pctUsed < 0) pctUsed = 0;
                    if (pctUsed > 100) pctUsed = 100;

                    prgRam.Value = pctUsed;
                    lblRamStats.Text = string.Format("RAM: {0} MB / {1} MB (Còn trống: {2} MB - {3}% Rảnh)",
                        totalRam - availRam, totalRam, availRam, 100 - pctUsed);

                    int estGames = availRam / 70;
                    if (estGames < 0) estGames = 0;
                    lblCapacityHint.Text = string.Format("💡 Đánh giá sức chứa: Còn trống {0} MB RAM, có thể treo thêm ~{1} game nữa!", availRam, estGames);
                }

                Match mSwap = Regex.Match(raw, @"Swap:\s+(\d+)\s+(\d+)\s+(\d+)");
                if (mSwap.Success)
                {
                    int totalSwap = int.Parse(mSwap.Groups[1].Value);
                    int usedSwap = int.Parse(mSwap.Groups[2].Value);
                    lblSwapStats.Text = string.Format("Swap: {0} MB / {1} MB", usedSwap, totalSwap);
                }

                // 2. Parse Load Average
                if (raw.Contains("---LOAD---"))
                {
                    string afterLoad = raw.Substring(raw.IndexOf("---LOAD---") + 10);
                    string loadLine = afterLoad.Split(new[] { "---SLOTS---" }, StringSplitOptions.None)[0].Trim();
                    lblCpuStats.Text = "CPU Load: " + loadLine;
                }

                // 3. Parse Slots
                lvwSlots.Items.Clear();
                if (raw.Contains("---SLOTS---"))
                {
                    string slotsPart = raw.Substring(raw.IndexOf("---SLOTS---") + 11);
                    string slotsJson = slotsPart.Split(new[] { "---PROCESSES---" }, StringSplitOptions.None)[0].Trim();
                    string procsPart = slotsPart.Contains("---PROCESSES---") ? slotsPart.Substring(slotsPart.IndexOf("---PROCESSES---") + 15) : "";

                    if (!string.IsNullOrEmpty(slotsJson) && slotsJson.StartsWith("{"))
                    {
                        JavaScriptSerializer js = new JavaScriptSerializer();
                        Dictionary<string, object> slots = js.Deserialize<Dictionary<string, object>>(slotsJson);
                        if (slots != null)
                        {
                            foreach (var kvp in slots)
                            {
                                var item = kvp.Value as Dictionary<string, object>;
                                if (item == null) continue;

                                int sid = item.ContainsKey("slot") ? Convert.ToInt32(item["slot"]) : int.Parse(kvp.Key);
                                string game = item.ContainsKey("game") ? item["game"].ToString() : "N/A";
                                string acc = item.ContainsKey("acc") ? item["acc"].ToString() : "slot" + sid;
                                string pin = item.ContainsKey("pin") ? item["pin"].ToString() : "N/A";
                                int port = item.ContainsKey("port") ? Convert.ToInt32(item["port"]) : 5900 + sid;

                                bool isRunning = procsPart.Contains(acc) || procsPart.Contains(game);
                                string status = isRunning ? "✔ Đang cày 24/7" : "■ Đã tắt";

                                ListViewItem lvi = new ListViewItem("Slot " + sid);
                                lvi.SubItems.Add(game);
                                lvi.SubItems.Add(acc);
                                lvi.SubItems.Add(pin);
                                lvi.SubItems.Add(port.ToString());
                                lvi.SubItems.Add(status);
                                lvi.Tag = sid;

                                if (isRunning) lvi.ForeColor = Color.FromArgb(22, 163, 74);
                                else lvi.ForeColor = Color.FromArgb(100, 116, 139);

                                lvwSlots.Items.Add(lvi);
                            }
                        }
                    }

                    // Nếu chưa có slot nào trong slots.json, kiểm tra game mặc định
                    if (lvwSlots.Items.Count == 0)
                    {
                        bool isDefRunning = procsPart.Contains("microemulator");
                        ListViewItem lvi = new ListViewItem("Slot 1 (Gốc)");
                        lvi.SubItems.Add(currentVps.GameTitle);
                        lvi.SubItems.Add("acc1");
                        lvi.SubItems.Add(currentVps.DefaultPin ?? "1234");
                        lvi.SubItems.Add("5901");
                        lvi.SubItems.Add(isDefRunning ? "✔ Đang cày 24/7" : "■ Đã dừng");
                        lvi.Tag = 1;
                        if (isDefRunning) lvi.ForeColor = Color.FromArgb(22, 163, 74);
                        lvwSlots.Items.Add(lvi);
                    }
                }
            }
            catch { }
        }

        private string ResolveSshExe()
        {
            string winSsh = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "OpenSSH", "ssh.exe");
            if (File.Exists(winSsh)) return winSsh;
            return "ssh.exe";
        }

        private string ExecuteSsh(VpsInfo vps, string cmd)
        {
            try
            {
                string key = vps.Key.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                string sshArgs = string.Format("-i \"{0}\" -p {1} -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o ConnectTimeout=6 {2}@{3} \"{4}\"",
                    key, vps.Port, vps.User, vps.Host, cmd);

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = ResolveSshExe(),
                    Arguments = sshArgs,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (Process p = Process.Start(psi))
                {
                    string stdout = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(7000);
                    return stdout;
                }
            }
            catch (Exception ex)
            {
                return "ERR:" + ex.Message;
            }
        }

        private void ViewSelectedSlotGame()
        {
            if (currentVps == null || lvwSlots.SelectedItems.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn 1 slot game trong bảng để xem!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ListViewItem sel = lvwSlots.SelectedItems[0];
            int slotId = Convert.ToInt32(sel.Tag);
            int vncRemotePort = 5900 + slotId;

            ThreadPool.QueueUserWorkItem(state =>
            {
                try
                {
                    int localPort = 5900 + slotId;
                    for (int p = localPort; p < localPort + 40; p++)
                    {
                        if (IsPortFree(p)) { localPort = p; break; }
                    }

                    // Tắt tunnel cũ nếu có
                    if (activeTunnel != null && !activeTunnel.HasExited)
                    {
                        try { activeTunnel.Kill(); } catch { }
                    }

                    string key = currentVps.Key.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                    string sshArgs = string.Format("-i \"{0}\" -p {1} -N -L {2}:localhost:{3} -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o ConnectTimeout=6 {4}@{5}",
                        key, currentVps.Port, localPort, vncRemotePort, currentVps.User, currentVps.Host);

                    ProcessStartInfo psiTunnel = new ProcessStartInfo
                    {
                        FileName = ResolveSshExe(),
                        Arguments = sshArgs,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    activeTunnel = Process.Start(psiTunnel);
                    Thread.Sleep(800);

                    string vncExe = Path.Combine(projectDir, "vncviewer.exe");
                    if (!File.Exists(vncExe))
                    {
                        vncExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vncviewer.exe");
                    }

                    if (!File.Exists(vncExe))
                    {
                        SafeInvoke(() => MessageBox.Show("Không tìm thấy file vncviewer.exe trong thư mục dự án!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error));
                        return;
                    }

                    Process.Start(vncExe, string.Format("localhost:{0} -PreferredEncoding=Tight -NoJPEG=1 -CompressLevel=1", localPort));
                }
                catch (Exception ex)
                {
                    SafeInvoke(() => MessageBox.Show("Lỗi kết nối xem game: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error));
                }
            });
        }

        private bool IsPortFree(int port)
        {
            try
            {
                TcpListener l = new TcpListener(System.Net.IPAddress.Loopback, port);
                l.Start();
                l.Stop();
                return true;
            }
            catch { return false; }
        }

        private void RestartSelectedSlot()
        {
            if (currentVps == null || lvwSlots.SelectedItems.Count == 0) return;
            int slotId = Convert.ToInt32(lvwSlots.SelectedItems[0].Tag);
            string acc = "slot" + slotId;

            ThreadPool.QueueUserWorkItem(state =>
            {
                ExecuteSsh(currentVps, string.Format("/opt/jar-manager/scripts/stop-acc.sh {0}; sleep 1; /opt/jar-manager/scripts/start-acc.sh {0} /opt/jar-manager/games/game.jar {1}", acc, slotId));
                SafeInvoke(() =>
                {
                    MessageBox.Show(string.Format("Đã gửi lệnh khởi động lại Slot {0}!", slotId), "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    OnSelectedVpsChanged();
                });
            });
        }

        private void RemoveSelectedSlot()
        {
            if (currentVps == null || lvwSlots.SelectedItems.Count == 0) return;
            int slotId = Convert.ToInt32(lvwSlots.SelectedItems[0].Tag);

            if (slotId == 1)
            {
                var warn1 = MessageBox.Show("CẢNH BÁO: Slot 1 là slot game chính (mặc định) của VPS.\nBạn có chắc chắn muốn xóa không?", "Cảnh báo Slot gốc", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (warn1 != DialogResult.Yes) return;
            }

            var confirm = MessageBox.Show(string.Format("Bạn có chắc muốn XÓA HOÀN TOÀN Slot {0}?\nKhách cũ sẽ bị thu hồi mã PIN và dữ liệu RMS sẽ bị xóa sạch!", slotId),
                "Xác nhận xóa slot khách", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            ThreadPool.QueueUserWorkItem(state =>
            {
                ExecuteSsh(currentVps, string.Format("bash /opt/jar-manager/scripts/slot-manager.sh remove {0}", slotId));
                SafeInvoke(() =>
                {
                    MessageBox.Show(string.Format("Đã xóa hoàn toàn Slot {0} và thu hồi mã PIN!", slotId), "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    OnSelectedVpsChanged();
                });
            });
        }

        private void ShowAddSlotDialog()
        {
            if (currentVps == null) return;

            using (Form f = new Form())
            {
                f.Text = "Thêm Slot Game Mới Cho Khách Trên " + currentVps.Id;
                f.Size = new Size(420, 260);
                f.StartPosition = FormStartPosition.CenterParent;
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.MaximizeBox = false;

                Label l1 = new Label { Text = "Số thứ tự Slot (ví dụ: 2, 3):", Location = new Point(20, 15), AutoSize = true };
                TextBox txtSlot = new TextBox { Text = (lvwSlots.Items.Count + 1).ToString(), Location = new Point(20, 35), Width = 100 };

                Label l2 = new Label { Text = "Tên Game (ví dụ: Avatar 250):", Location = new Point(20, 65), AutoSize = true };
                TextBox txtGame = new TextBox { Text = "Game Moi", Location = new Point(20, 85), Width = 360 };

                Label l3 = new Label { Text = "Mã PIN cấp cho khách (4 số):", Location = new Point(20, 115), AutoSize = true };
                TextBox txtPin = new TextBox { Text = new Random().Next(1000, 9999).ToString(), Location = new Point(20, 135), Width = 150 };

                Button btnOk = new Button { Text = "Kích Hoạt Slot", Location = new Point(240, 175), Size = new Size(140, 32), BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
                btnOk.Click += (s, e) =>
                {
                    int sid;
                    if (!int.TryParse(txtSlot.Text.Trim(), out sid)) { MessageBox.Show("Số slot không hợp lệ!"); return; }
                    string gName = txtGame.Text.Trim().Replace("'", "").Replace("\"", "");
                    string pin = txtPin.Text.Trim();

                    f.DialogResult = DialogResult.OK;
                    f.Close();

                    ThreadPool.QueueUserWorkItem(state =>
                    {
                        ExecuteSsh(currentVps, string.Format("bash /opt/jar-manager/scripts/slot-manager.sh add {0} /opt/jar-manager/games/game.jar {1} '{2}'", sid, pin, gName));
                        SafeInvoke(() =>
                        {
                            MessageBox.Show(string.Format("Đã kích hoạt Slot {0} ({1}) với mã PIN: {2}", sid, gName, pin), "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            OnSelectedVpsChanged();
                        });
                    });
                };

                f.Controls.AddRange(new Control[] { l1, txtSlot, l2, txtGame, l3, txtPin, btnOk });
                f.ShowDialog(this);
            }
        }

        private void CopyIphoneLink()
        {
            if (currentVps == null) return;
            string link = !string.IsNullOrEmpty(currentVps.WebUrl) ? currentVps.WebUrl : "https://mistily-stipular-kristopher.ngrok-free.dev";
            if (lvwSlots.SelectedItems.Count > 0)
            {
                string pin = lvwSlots.SelectedItems[0].SubItems[3].Text;
                if (!string.IsNullOrEmpty(pin) && pin != "N/A")
                {
                    link = link.TrimEnd('/') + "/?pin=" + pin;
                }
            }
            else if (!string.IsNullOrEmpty(currentVps.DefaultPin))
            {
                link = link.TrimEnd('/') + "/?pin=" + currentVps.DefaultPin;
            }
            Clipboard.SetText(link);
            MessageBox.Show("Đã copy link Safari cho iPhone vào Clipboard:\n" + link, "Copy thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ExportExeForClient()
        {
            if (currentVps == null) return;
            try
            {
                string pythonExe = "python.exe";
                string script = Path.Combine(projectDir, "tools", "package_client.py");
                Process.Start(pythonExe, string.Format("\"{0}\" --host {1} --port {2} --title \"{3}\" --push-pubkey",
                    script, currentVps.Host, currentVps.Port, currentVps.GameTitle));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khởi chạy đóng gói: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowAddVpsDialog()
        {
            MessageBox.Show("Để thêm VPS mới vào danh bạ, hãy cập nhật thông tin trong file 'vps_registry.json' hoặc dùng lệnh:\npython tools/vps_manager.py list",
                "Hướng dẫn thêm VPS", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
