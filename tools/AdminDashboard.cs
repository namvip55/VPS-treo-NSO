using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
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
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("shcore.dll")]
        private static extern int SetProcessDpiAwareness(int awareness);

        [STAThread]
        static void Main()
        {
            // Bật chuẩn DPI Per-Monitor v2 từ Windows
            try
            {
                if (Environment.OSVersion.Version.Major >= 6)
                {
                    SetProcessDpiAwareness(2); // PROCESS_PER_MONITOR_DPI_AWARE
                }
            }
            catch
            {
                try { SetProcessDPIAware(); } catch { }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new DashboardForm());
        }
    }

    public class ModernProgressBar : Control
    {
        private int val = 0;
        private string textOverlay = "";

        public int Value
        {
            get { return val; }
            set
            {
                val = Math.Max(0, Math.Min(100, value));
                this.Invalidate();
            }
        }

        public string TextOverlay
        {
            get { return textOverlay; }
            set
            {
                textOverlay = value;
                this.Invalidate();
            }
        }

        public ModernProgressBar()
        {
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.BackColor = Color.FromArgb(15, 23, 42);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Nền thanh bar
            using (SolidBrush bgBrush = new SolidBrush(this.BackColor))
            {
                g.FillRectangle(bgBrush, this.ClientRectangle);
            }

            // Phần trăm tiến trình với dải màu Gradient hiện đại
            if (val > 0)
            {
                int fillWidth = Math.Max(2, (int)((float)this.Width * val / 100f));
                Rectangle fillRect = new Rectangle(0, 0, fillWidth, this.Height);

                Color c1 = Color.FromArgb(59, 130, 246);  // Electric Blue
                Color c2 = Color.FromArgb(16, 185, 129); // Emerald Green
                if (val > 80)
                {
                    c1 = Color.FromArgb(245, 158, 11);  // Amber
                    c2 = Color.FromArgb(239, 68, 68);   // Crimson Red
                }

                using (LinearGradientBrush fillBrush = new LinearGradientBrush(fillRect, c1, c2, LinearGradientMode.Horizontal))
                {
                    g.FillRectangle(fillBrush, fillRect);
                }
            }

            // Viền bo tinh tế
            using (Pen borderPen = new Pen(Color.FromArgb(51, 65, 85), 1))
            {
                g.DrawRectangle(borderPen, 0, 0, this.Width - 1, this.Height - 1);
            }

            // Chữ hiển thị giữa thanh bar
            if (!string.IsNullOrEmpty(textOverlay))
            {
                using (Font f = new Font("Segoe UI", 8.75f, FontStyle.Bold))
                using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                using (SolidBrush textBrush = new SolidBrush(Color.White))
                {
                    SizeF sf = g.MeasureString(textOverlay, f);
                    float tx = (this.Width - sf.Width) / 2f;
                    float ty = (this.Height - sf.Height) / 2f;
                    g.DrawString(textOverlay, f, shadowBrush, tx + 1, ty + 1);
                    g.DrawString(textOverlay, f, textBrush, tx, ty);
                }
            }
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

    public class DashboardForm : Form
    {
        private float dpiScale = 1.0f;
        private int S(int val) { return (int)Math.Round(val * dpiScale); }

        private string projectDir;
        private string registryPath;
        private Dictionary<string, VpsInfo> servers = new Dictionary<string, VpsInfo>();
        private VpsInfo currentVps = null;

        // UI Controls
        private Panel pnlSidebar;
        private Panel pnlContent;
        private Label lblSideBrand;
        private Label lblSideSub;
        private Button btnRefreshAll;
        private ListBox lstVps;
        private Button btnAddVps;

        // Header Controls
        private Panel pnlHeader;
        private Label lblHeaderTitle;
        private Label lblHeaderSub;
        private Label lblStatusBadge;
        private Button btnCopyIphoneLink;
        private Button btnExportExe;

        // Hardware Controls
        private Panel pnlHw;
        private Label lblHwHeader;
        private Label lblRamTitle;
        private ModernProgressBar prgRam;
        private Label lblSwapStats;
        private Label lblCpuStats;
        private Label lblCapacityHint;

        // Slot Controls
        private Panel pnlSlotsCard;
        private Label lblSlotsHeader;
        private ListView lvwSlots;
        private Panel pnlSlotActions;
        private Button btnViewGame;
        private Button btnRestartSlot;
        private Button btnDeleteSlot;
        private Button btnAddSlot;

        private Process activeTunnel = null;
        private ImageList imgSpacer = null;

        public DashboardForm()
        {
            // Đo đạc DPI thực tế của màn hình (192 DPI cho 200% scale)
            using (Graphics g = this.CreateGraphics())
            {
                dpiScale = g.DpiX / 96.0f;
                if (dpiScale < 1.0f) dpiScale = 1.0f;
            }

            // Tắt auto-scale của WinForms để kiểm soát layout pixel hoàn hảo
            this.AutoScaleMode = AutoScaleMode.None;

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
            this.Text = "JarManager Central Admin 24/7 - Giám Sát VPS & Quản Lý Slot Game Mobile";
            
            // Tính toán kích thước cửa sổ linh hoạt theo không gian làm việc của màn hình
            Rectangle workArea = Screen.PrimaryScreen.WorkingArea;
            int targetW = S(1150);
            int targetH = S(720);
            if (targetW > workArea.Width - S(40)) targetW = workArea.Width - S(40);
            if (targetH > workArea.Height - S(50)) targetH = workArea.Height - S(50);

            this.Size = new Size(targetW, targetH);
            this.MinimumSize = new Size(S(920), S(580));
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(11, 15, 25); // Deep Midnight Obsidian
            this.ForeColor = Color.FromArgb(241, 245, 249);
            this.Font = new Font("Segoe UI", 9.25f);

            // ==========================================
            // 1. SIDEBAR BÊN TRÁI
            // ==========================================
            pnlSidebar = new Panel
            {
                BackColor = Color.FromArgb(17, 24, 39)
            };
            this.Controls.Add(pnlSidebar);

            lblSideBrand = new Label
            {
                Text = "JARMANAGER 24/7",
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(59, 130, 246), // Electric Royal Blue
                UseMnemonic = false,
                AutoSize = true
            };
            pnlSidebar.Controls.Add(lblSideBrand);

            lblSideSub = new Label
            {
                Text = "Danh Sách Máy Chủ (VPS)",
                Font = new Font("Segoe UI", 8.75f),
                ForeColor = Color.FromArgb(148, 163, 184),
                UseMnemonic = false,
                AutoSize = true
            };
            pnlSidebar.Controls.Add(lblSideSub);

            btnRefreshAll = new Button
            {
                Text = "↺  Làm Mới Trạng Thái",
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.FromArgb(226, 232, 240),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRefreshAll.FlatAppearance.BorderSize = 0;
            btnRefreshAll.Click += (s, e) => LoadRegistryAndCheckAll();
            pnlSidebar.Controls.Add(btnRefreshAll);

            lstVps = new ListBox
            {
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.FromArgb(241, 245, 249),
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5f),
                ItemHeight = S(48),
                DrawMode = DrawMode.OwnerDrawFixed
            };
            lstVps.DrawItem += LstVps_DrawItem;
            lstVps.SelectedIndexChanged += (s, e) => OnSelectedVpsChanged();
            pnlSidebar.Controls.Add(lstVps);

            btnAddVps = new Button
            {
                Text = "+ Thêm VPS Mới",
                BackColor = Color.FromArgb(37, 99, 235), // Electric Blue
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAddVps.FlatAppearance.BorderSize = 0;
            btnAddVps.Click += (s, e) => ShowAddVpsDialog();
            pnlSidebar.Controls.Add(btnAddVps);

            // ==========================================
            // 2. PHÂN VÙNG NỘI DUNG CHÍNH (CONTENT)
            // ==========================================
            pnlContent = new Panel
            {
                BackColor = Color.FromArgb(11, 15, 25)
            };
            this.Controls.Add(pnlContent);

            // --- 2.1 HEADER CARD ---
            pnlHeader = new Panel
            {
                BackColor = Color.FromArgb(22, 31, 48)
            };
            pnlContent.Controls.Add(pnlHeader);

            lblHeaderTitle = new Label
            {
                Text = "Chưa có VPS nào được chọn",
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = Color.White,
                UseMnemonic = false,
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblHeaderTitle);

            lblHeaderSub = new Label
            {
                Text = "Host: - | Port NAT: -",
                Font = new Font("Segoe UI", 8.75f),
                ForeColor = Color.FromArgb(148, 163, 184),
                UseMnemonic = false,
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblHeaderSub);

            lblStatusBadge = new Label
            {
                Text = "[○] ĐANG CHỜ...",
                Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                UseMnemonic = false,
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblStatusBadge);

            btnCopyIphoneLink = new Button
            {
                Text = "Link Safari Mobile",
                BackColor = Color.FromArgb(16, 185, 129), // Emerald
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCopyIphoneLink.FlatAppearance.BorderSize = 0;
            btnCopyIphoneLink.Click += (s, e) => CopyIphoneLink();
            pnlHeader.Controls.Add(btnCopyIphoneLink);

            btnExportExe = new Button
            {
                Text = "Xuất App Windows",
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnExportExe.FlatAppearance.BorderSize = 0;
            btnExportExe.Click += (s, e) => ExportExeForClient();
            pnlHeader.Controls.Add(btnExportExe);

            // --- 2.2 HARDWARE CARD ---
            pnlHw = new Panel
            {
                BackColor = Color.FromArgb(22, 31, 48)
            };
            pnlContent.Controls.Add(pnlHw);

            lblHwHeader = new Label
            {
                Text = "THÔNG SỐ PHẦN CỨNG & TÀI NGUYÊN MÁY CHỦ",
                Font = new Font("Segoe UI", 9.75f, FontStyle.Bold),
                ForeColor = Color.FromArgb(248, 250, 252),
                UseMnemonic = false,
                AutoSize = true
            };
            pnlHw.Controls.Add(lblHwHeader);

            lblRamTitle = new Label
            {
                Text = "RAM: Đang đo...",
                Font = new Font("Segoe UI", 8.75f),
                ForeColor = Color.FromArgb(203, 213, 225),
                UseMnemonic = false,
                AutoSize = true
            };
            pnlHw.Controls.Add(lblRamTitle);

            prgRam = new ModernProgressBar
            {
                Value = 0,
                TextOverlay = "0%"
            };
            pnlHw.Controls.Add(prgRam);

            lblSwapStats = new Label
            {
                Text = "Swap: -",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(148, 163, 184),
                UseMnemonic = false,
                AutoSize = true
            };
            pnlHw.Controls.Add(lblSwapStats);

            lblCpuStats = new Label
            {
                Text = "CPU Load: -",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(148, 163, 184),
                UseMnemonic = false,
                AutoSize = true
            };
            pnlHw.Controls.Add(lblCpuStats);

            lblCapacityHint = new Label
            {
                Text = "Sức chứa: Vui lòng kết nối để phân tích dung lượng trống...",
                Font = new Font("Segoe UI", 8.75f, FontStyle.Italic),
                ForeColor = Color.FromArgb(52, 211, 153),
                UseMnemonic = false,
                AutoSize = true
            };
            pnlHw.Controls.Add(lblCapacityHint);

            // --- 2.3 GAME SLOTS CARD ---
            pnlSlotsCard = new Panel
            {
                BackColor = Color.FromArgb(22, 31, 48)
            };
            pnlContent.Controls.Add(pnlSlotsCard);

            lblSlotsHeader = new Label
            {
                Text = "DANH SÁCH GAME & SLOT (CHIA SẺ 1 LINK NGROK)",
                Font = new Font("Segoe UI", 9.75f, FontStyle.Bold),
                ForeColor = Color.FromArgb(248, 250, 252),
                UseMnemonic = false,
                AutoSize = true
            };
            pnlSlotsCard.Controls.Add(lblSlotsHeader);

            // Spacer trick cho ListView row height
            imgSpacer = new ImageList();
            imgSpacer.ImageSize = new Size(1, S(28));

            lvwSlots = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.FromArgb(241, 245, 249),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9f),
                MultiSelect = false,
                SmallImageList = imgSpacer
            };
            lvwSlots.Columns.Add("SLOT", S(65));
            lvwSlots.Columns.Add("TỰA GAME", S(160));
            lvwSlots.Columns.Add("ACC / PROFILE", S(110));
            lvwSlots.Columns.Add("MÃ PIN", S(80));
            lvwSlots.Columns.Add("PORT VNC", S(85));
            lvwSlots.Columns.Add("TRẠNG THÁI", S(140));
            pnlSlotsCard.Controls.Add(lvwSlots);

            pnlSlotActions = new Panel
            {
                BackColor = Color.Transparent
            };
            pnlSlotsCard.Controls.Add(pnlSlotActions);

            btnViewGame = new Button
            {
                Text = "▶  Xem Live (VNC)",
                BackColor = Color.FromArgb(16, 185, 129), // Emerald
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnViewGame.FlatAppearance.BorderSize = 0;
            btnViewGame.Click += (s, e) => ViewSelectedSlotGame();
            pnlSlotActions.Controls.Add(btnViewGame);

            btnRestartSlot = new Button
            {
                Text = "↺  Khởi Động Lại",
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.FromArgb(241, 245, 249),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRestartSlot.FlatAppearance.BorderSize = 0;
            btnRestartSlot.Click += (s, e) => RestartSelectedSlot();
            pnlSlotActions.Controls.Add(btnRestartSlot);

            btnDeleteSlot = new Button
            {
                Text = "×  Xóa Slot Khách",
                BackColor = Color.FromArgb(153, 27, 27),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDeleteSlot.FlatAppearance.BorderSize = 0;
            btnDeleteSlot.Click += (s, e) => RemoveSelectedSlot();
            pnlSlotActions.Controls.Add(btnDeleteSlot);

            btnAddSlot = new Button
            {
                Text = "+ Thêm Slot Mới",
                BackColor = Color.FromArgb(37, 99, 235), // Electric Blue
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAddSlot.FlatAppearance.BorderSize = 0;
            btnAddSlot.Click += (s, e) => ShowAddSlotDialog();
            pnlSlotActions.Controls.Add(btnAddSlot);

            // ==========================================
            // 3. FLUID RESPONSIVE ENGINE (CHỐNG MÁY / TRÀN VIỀN)
            // ==========================================
            Action doLayout = () =>
            {
                int clientW = this.ClientSize.Width;
                int clientH = this.ClientSize.Height;
                if (clientW <= 0 || clientH <= 0) return;

                // 3.1 Sidebar & Content bounds độc lập (Tránh lỗi đè của WinForms Docking)
                int sideW = Math.Max(S(220), Math.Min(S(280), (int)(clientW * 0.25f)));
                pnlSidebar.SetBounds(0, 0, sideW, clientH);
                pnlContent.SetBounds(sideW, 0, clientW - sideW, clientH);

                // 3.2 Sidebar interior layout
                lblSideBrand.Location = new Point(S(12), S(12));
                lblSideSub.Location = new Point(S(12), S(34));
                btnRefreshAll.SetBounds(S(12), S(58), sideW - S(24), S(32));
                btnAddVps.SetBounds(S(12), pnlSidebar.ClientSize.Height - S(44), sideW - S(24), S(34));
                lstVps.SetBounds(S(12), S(96), sideW - S(24), pnlSidebar.ClientSize.Height - S(96) - S(52));

                // 3.3 Content interior layout
                int pad = S(10);
                int cardW = pnlContent.ClientSize.Width - (pad * 2);

                // Header Card
                int headerH = S(82);
                pnlHeader.SetBounds(pad, pad, cardW, headerH);
                lblHeaderTitle.Location = new Point(S(14), S(10));
                lblHeaderSub.Location = new Point(S(14), S(34));
                lblStatusBadge.Location = new Point(S(14), S(56));

                int btnH = S(32);
                int btnExeW = S(135);
                int btnWebW = S(140);
                btnExportExe.SetBounds(pnlHeader.ClientSize.Width - btnExeW - S(14), S(25), btnExeW, btnH);
                btnCopyIphoneLink.SetBounds(btnExportExe.Left - S(8) - btnWebW, S(25), btnWebW, btnH);

                // Hardware Card
                int hwH = S(138);
                pnlHw.SetBounds(pad, pnlHeader.Bottom + pad, cardW, hwH);
                lblHwHeader.Location = new Point(S(14), S(10));
                lblRamTitle.Location = new Point(S(14), S(32));
                prgRam.SetBounds(S(14), S(54), pnlHw.ClientSize.Width - S(28), S(22));
                lblSwapStats.Location = new Point(S(14), S(84));
                lblCpuStats.Location = new Point(pnlHw.ClientSize.Width / 2, S(84));
                lblCapacityHint.Location = new Point(S(14), S(110));

                // Slots Card
                int slotsTop = pnlHw.Bottom + pad;
                int slotsH = pnlContent.ClientSize.Height - slotsTop - pad;
                pnlSlotsCard.SetBounds(pad, slotsTop, cardW, slotsH);
                lblSlotsHeader.Location = new Point(S(14), S(10));

                int actH = S(32);
                pnlSlotActions.SetBounds(S(14), pnlSlotsCard.ClientSize.Height - actH - S(10), pnlSlotsCard.ClientSize.Width - S(28), actH);

                int b1W = S(145);
                int b2W = S(110);
                int b3W = S(110);
                int b4W = S(135);
                btnViewGame.SetBounds(0, 0, b1W, actH);
                btnRestartSlot.SetBounds(btnViewGame.Right + S(8), 0, b2W, actH);
                btnDeleteSlot.SetBounds(btnRestartSlot.Right + S(8), 0, b3W, actH);
                btnAddSlot.SetBounds(pnlSlotActions.ClientSize.Width - b4W, 0, b4W, actH);

                int lTop = S(34);
                int lH = pnlSlotActions.Top - lTop - S(8);
                lvwSlots.SetBounds(S(14), lTop, pnlSlotsCard.ClientSize.Width - S(28), lH);

                // Tự động kéo giãn cột Trạng Thái để phủ 100% chiều ngang ListView
                if (lvwSlots.Columns.Count >= 6)
                {
                    int fixedW = S(65) + S(160) + S(110) + S(80) + S(85);
                    int remaining = lvwSlots.ClientSize.Width - fixedW - S(4);
                    lvwSlots.Columns[5].Width = Math.Max(S(140), remaining);
                }
            };

            this.Resize += (s, e) => doLayout();
            doLayout();

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

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Rectangle rect = e.Bounds;

            // Nền thẻ Card VPS
            Color bgColor = isSelected ? Color.FromArgb(30, 58, 138) : (e.Index % 2 == 0 ? Color.FromArgb(17, 24, 39) : Color.FromArgb(15, 23, 42));
            using (SolidBrush b = new SolidBrush(bgColor))
            {
                g.FillRectangle(b, rect);
            }

            // Dải highlight bên trái khi chọn
            if (isSelected)
            {
                using (SolidBrush bHighlight = new SolidBrush(Color.FromArgb(59, 130, 246)))
                {
                    g.FillRectangle(bHighlight, rect.X, rect.Y, S(3), rect.Height);
                }
            }

            // Chấm trạng thái Online/Offline
            Color dotColor = (info != null && info.IsOnline) ? Color.FromArgb(16, 185, 129) : Color.FromArgb(239, 68, 68);
            using (SolidBrush dotBrush = new SolidBrush(dotColor))
            {
                int dotSize = S(8);
                g.FillEllipse(dotBrush, rect.X + S(12), rect.Y + (rect.Height - dotSize) / 2, dotSize, dotSize);
            }

            // Tên máy chủ (Title)
            string title = info != null ? info.Name : key;
            using (Font titleFont = new Font("Segoe UI", 9.25f, FontStyle.Bold))
            using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(248, 250, 252)))
            {
                g.DrawString(title, titleFont, titleBrush, rect.X + S(26), rect.Y + S(6));
            }

            // Phụ đề (Host:Port)
            string sub = info != null ? string.Format("{0}:{1}", info.Host, info.Port) : "";
            if (info != null && info.IsOnline && info.PingMs > 0)
            {
                sub += string.Format(" • {0}ms", info.PingMs);
            }
            using (Font subFont = new Font("Segoe UI", 8.25f))
            using (SolidBrush subBrush = new SolidBrush(Color.FromArgb(148, 163, 184)))
            {
                g.DrawString(sub, subFont, subBrush, rect.X + S(26), rect.Y + S(24));
            }

            // Đường kẻ đáy phân cách
            using (Pen linePen = new Pen(Color.FromArgb(30, 41, 59), 1))
            {
                g.DrawLine(linePen, rect.X, rect.Bottom - 1, rect.Right, rect.Bottom - 1);
            }
        }

        private void LoadRegistryAndCheckAll()
        {
            servers.Clear();
            lstVps.Items.Clear();

            if (!File.Exists(registryPath))
            {
                lblHeaderTitle.Text = "Chưa có VPS nào trong danh bạ";
                lblHeaderSub.Text = "Bấm nút '+ Thêm VPS Mới' để bắt đầu";
                return;
            }

            try
            {
                string json = File.ReadAllText(registryPath);
                JavaScriptSerializer js = new JavaScriptSerializer();
                Dictionary<string, object> dict = js.Deserialize<Dictionary<string, object>>(json);

                Dictionary<string, object> srvDict = null;
                if (dict != null)
                {
                    if (dict.ContainsKey("servers"))
                    {
                        srvDict = dict["servers"] as Dictionary<string, object>;
                    }
                    else
                    {
                        srvDict = dict;
                    }
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
                else
                {
                    lblHeaderTitle.Text = "Hệ thống đang trống";
                    lblHeaderSub.Text = "Vui lòng thêm VPS mới để bắt đầu quản lý";
                    lblRamTitle.Text = "RAM: -";
                    prgRam.Value = 0;
                    prgRam.TextOverlay = "0%";
                    lblSwapStats.Text = "Swap: -";
                    lblCpuStats.Text = "CPU Load: -";
                    lblCapacityHint.Text = "Đánh giá sức chứa: Hệ thống sẵn sàng tiếp nhận VPS mới.";
                    lvwSlots.Items.Clear();
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
                lblStatusBadge.Text = string.Format("● ONLINE ({0} ms)", currentVps.PingMs);
                lblStatusBadge.ForeColor = Color.FromArgb(52, 211, 153); // Emerald
            }
            else
            {
                lblStatusBadge.Text = "○ OFFLINE (Chưa thể kết nối)";
                lblStatusBadge.ForeColor = Color.FromArgb(248, 113, 113); // Red
            }

            // Xóa dữ liệu cũ
            lblRamTitle.Text = "RAM: Đang kết nối SSH đọc thông số...";
            prgRam.Value = 0;
            prgRam.TextOverlay = "Đang đọc...";
            lvwSlots.Items.Clear();

            // Nạp thông số chi tiết qua SSH ngầm
            ThreadPool.QueueUserWorkItem(state => FetchVpsDetailsAsync(currentVps));
        }

        private void FetchVpsDetailsAsync(VpsInfo vps)
        {
            string remoteCmd = "free -m; echo '---LOAD---'; cat /proc/loadavg 2>/dev/null; echo '---SLOTS---'; cat /opt/jar-manager/slots.json 2>/dev/null || echo '{}'; echo '---PROCESSES---'; ps -eo pid,cmd | grep java | grep -v grep";
            string output = ExecuteSsh(vps, remoteCmd);

            SafeInvoke(() =>
            {
                if (currentVps != vps) return;
                ParseAndUpdateVpsStats(output);
            });
        }

        private void ParseAndUpdateVpsStats(string raw)
        {
            if (string.IsNullOrEmpty(raw) || raw.StartsWith("ERR:"))
            {
                lblRamTitle.Text = "RAM: Không thể lấy dữ liệu (Mất kết nối SSH hoặc VPS tắt)";
                prgRam.Value = 0;
                prgRam.TextOverlay = "Không có tín hiệu";
                lblCapacityHint.Text = "Đánh giá sức chứa: Vui lòng kiểm tra lại trạng thái mạng VPS.";
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
                    prgRam.TextOverlay = string.Format("{0}% ({1} MB / {2} MB)", pctUsed, totalRam - availRam, totalRam);
                    lblRamTitle.Text = string.Format("RAM: {0} MB / {1} MB (Còn trống: {2} MB - {3}% Rảnh)",
                        totalRam - availRam, totalRam, availRam, 100 - pctUsed);

                    int estGames = availRam / 70;
                    if (estGames < 0) estGames = 0;
                    lblCapacityHint.Text = string.Format("Đánh giá sức chứa: Còn trống {0} MB RAM, có thể treo thêm ~{1} game nữa!", availRam, estGames);
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

                                if (isRunning) lvi.ForeColor = Color.FromArgb(52, 211, 153); // Emerald
                                else lvi.ForeColor = Color.FromArgb(148, 163, 184);

                                lvwSlots.Items.Add(lvi);
                            }
                        }
                    }

                    // Nếu chưa có slot nào trong slots.json, hiển thị slot 1 mặc định
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
                        if (isDefRunning) lvi.ForeColor = Color.FromArgb(52, 211, 153);
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
                f.Size = new Size(S(440), S(280));
                f.StartPosition = FormStartPosition.CenterParent;
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.MaximizeBox = false;
                f.BackColor = Color.FromArgb(17, 24, 39);
                f.ForeColor = Color.White;
                f.Font = new Font("Segoe UI", 9.25f);

                Label l1 = new Label { Text = "Số thứ tự Slot (ví dụ: 2, 3):", Location = new Point(S(20), S(15)), AutoSize = true, ForeColor = Color.FromArgb(226, 232, 240), UseMnemonic = false };
                TextBox txtSlot = new TextBox { Text = (lvwSlots.Items.Count + 1).ToString(), Location = new Point(S(20), S(38)), Width = S(120), BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.White };

                Label l2 = new Label { Text = "Tên Game (ví dụ: Avatar 250):", Location = new Point(S(20), S(72)), AutoSize = true, ForeColor = Color.FromArgb(226, 232, 240), UseMnemonic = false };
                TextBox txtGame = new TextBox { Text = "Avatar 250", Location = new Point(S(20), S(95)), Width = S(380), BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.White };

                Label l3 = new Label { Text = "Mã PIN cấp cho khách (4 số):", Location = new Point(S(20), S(130)), AutoSize = true, ForeColor = Color.FromArgb(226, 232, 240), UseMnemonic = false };
                TextBox txtPin = new TextBox { Text = new Random().Next(1000, 9999).ToString(), Location = new Point(S(20), S(153)), Width = S(160), BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.White };

                Button btnOk = new Button
                {
                    Text = "Kích Hoạt Slot",
                    Location = new Point(S(240), S(195)),
                    Size = new Size(S(160), S(40)),
                    BackColor = Color.FromArgb(37, 99, 235),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
                };
                btnOk.FlatAppearance.BorderSize = 0;
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
            MessageBox.Show("Để thêm VPS mới vào danh bạ, hãy cập nhật thông tin theo mẫu tại file 'VPS_SPEC_TEMPLATE.md' hoặc dùng lệnh:\npython tools/vps_manager.py update-vps <name-vps> <host:port>",
                "Hướng dẫn thêm VPS", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
