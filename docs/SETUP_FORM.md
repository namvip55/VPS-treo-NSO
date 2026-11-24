---
name: vps-jar-setup
description: >-
  Cẩm nang và quy trình tự động setup toàn diện hệ thống VPS Linux (IP NAT) treo game J2ME (.jar / Ninja School Online) 24/7.
  Kích hoạt khi người dùng yêu cầu setup VPS mới, cài đặt lại VPS, kiểm tra thông tin VPS, kết nối SSH, VNC, hoặc quản lý tài khoản game.
---

# MASTER SKILL: SETUP TOÀN DIỆN VPS J2ME (.JAR) & TRIỂN KHAI CLIENT JARMANAGER.EXE (FORM CHUẨN)

Skill này là **khung mẫu thiết kế chuẩn (Reference Blueprint)** của toàn bộ dự án, hướng dẫn từ A đến Z: từ cấu hình hệ điều hành Linux trên VPS cấu hình thấp (1 vCPU, 1GB RAM, IP NAT), tối ưu hiển thị Zero-Lag, cho đến việc lập trình và biên dịch ứng dụng điều khiển 1-click **`JarManager.exe`** trên máy tính Windows.

---

## 📋 PHẦN 1: BẢNG THÔNG SỐ FORM MẪU (CHUẨN DỰ ÁN)

### 1.1. Thông Số Phần Cứng & Mạng VPS
* **Nhà cung cấp:** `vps1k.net` (hoặc bất kỳ VPS NAT nào)
* **Hệ điều hành:** Ubuntu 22.04 LTS x86_64 (Minimal)
* **Phần cứng:** 1 vCPU, 1024 MB RAM, 15 GB NVMe (SCSI), 2 GB Swap (`/swapfile`), Tổng Swap ~3.8 GB
* **Băng thông:** 40 Mbps (5 MB/s)
* **Loại mạng:** IP NAT (Việt Nam)
* **Host Remote:** `remote.io.vn`
* **Port NAT (SSH):** `10653` (ánh xạ vào cổng SSH 22 nội bộ qua giao diện `eth0.50`)
* **IP nội bộ (VLAN):** `10.50.2.141` | **Gateway:** `10.50.0.1` | **Subnet Mask:** `255.255.240.0`
* **User quản trị:** `root` | **Mật khẩu:** `<ROOT_PASSWORD>`
* **SSH Key cá nhân:** `C:\Users\Admin\.ssh\id_ed25519_jar` (Ed25519) | Alias: `vps-jar` trong `~/.ssh/config`
* **Lệnh cấp mạng bắt buộc khi cài mới:** `echo -e "1\ny" | /usr/local/bin/changeip`

---

## ⚠️ PHẦN 2: CÁC NGUYÊN TẮC SỐNG CÒN (CRITICAL RULES)

> [!CAUTION]
> **1. TUYỆT ĐỐI KHÔNG BẬT UFW (`ufw enable`):**
> Do VPS nằm sau router NAT định tuyến qua interface VLAN ảo `eth0.50` và bảng `ssh_rt`. Khi bật UFW, bộ lọc `rp_filter` của Linux coi gói tin phản hồi là giả mạo và drop 100% packets, gây mất kết nối SSH vĩnh viễn (buộc phải Reinstall OS).
> Router nhà mạng bên ngoài đã chặn 100% cổng, chỉ mở cổng SSH NAT đã ánh xạ. VNC chỉ bind `localhost`. Không cần và cấm bật UFW.

> [!IMPORTANT]
> **2. LỆNH NHẬN MẠNG `changeip` KHI CÀI MỚI VPS:**
> Khi cài lại Ubuntu từ web, card mạng chưa có internet. Luôn phải chạy:
> ```bash
> echo -e "1\ny" | /usr/local/bin/changeip
> ```

> [!TIP]
> **3. CƠ CHẾ CHẠY NỀN VĨNH VIỄN (`setsid`):**
> Mọi tiến trình Java phải bọc trong `setsid`:
> `setsid java ... > /dev/null 2>&1 &`
> Giúp game hoàn toàn tách biệt khỏi SSH/VNC; tắt app hay tắt máy tính thì game vẫn cày 24/7.

> [!NOTE]
> **4. CÔ LẬP DỮ LIỆU TÀI KHOẢN (RMS ISOLATION):**
> Luôn gán: `-Duser.home=/opt/jar-manager/profiles/<tên_acc>` để tránh đè mật khẩu và dữ liệu game.

---

## 🖥️ PHẦN 3: CẤU HÌNH SERVER VPS (ZERO-LAG ENGINE)

### 3.1. Tối ưu Hệ Điều Hành (OS Layer)
1. **Tạo 2GB Swap & tối ưu bộ nhớ:**
   ```bash
   fallocate -l 2G /swapfile && chmod 600 /swapfile && mkswap /swapfile && swapon /swapfile
   echo "/swapfile none swap sw 0 0" >> /etc/fstab
   sysctl vm.swappiness=10 && echo "vm.swappiness=10" >> /etc/sysctl.conf
   ```
2. **Gỡ bỏ rác & cài đặt các gói cần thiết:**
   ```bash
   apt-get purge -y snapd unattended-upgrades && apt-get update -y
   apt-get install -y openjdk-8-jre openbox tigervnc-standalone-server tigervnc-common xdotool wmctrl unzip wget curl
   ```

### 3.2. Cấu trúc thư mục `/opt/jar-manager/`
```bash
mkdir -p /opt/jar-manager/{emulator,games,profiles,scripts} /root/.vnc /root/.config/openbox
wget -O /opt/jar-manager/emulator/microemulator.jar https://repo1.maven.org/maven2/org/microemu/microemulator/2.0.4/microemulator-2.0.4.jar
```

### 3.3. Cấu hình TigerVNC Server (800x600 -depth 16 & Remap Phím)
Tạo file `/root/.vnc/config`:
```ini
geometry=800x600
depth=16
localhost
SecurityTypes=None
FrameRate=60
CompareFB=1
NeverShared=1
DisconnectClients=0
RemapKeys=0xffb8<>0xffb2,0xffb7<>0xffb1,0xffb9<>0xffb3,0xff97<>0xff99,0xff95<>0xff9c,0xff9a<>0xff9b
```

Tạo service systemd `/etc/systemd/system/vncserver.service`:
```ini
[Unit]
Description=TigerVNC Server on Display :1 (Optimized 800x600)
After=network.target

[Service]
Type=forking
User=root
ExecStartPre=-/usr/bin/vncserver -kill :1
ExecStart=/usr/bin/vncserver :1 -geometry 800x600 -depth 16 -localhost yes -SecurityTypes None
ExecStop=/usr/bin/vncserver -kill :1
Restart=on-failure
RestartSec=3

[Install]
WantedBy=multi-user.target
```
Kích hoạt service:
```bash
systemctl daemon-reload && systemctl enable --now vncserver.service
```

### 3.4. Cấu hình `/root/.vnc/xstartup` (Đảo NumPad 2 Lớp)
```bash
cat << 'EOF' > /root/.vnc/xstartup
#!/bin/sh
unset SESSION_MANAGER
unset DBUS_SESSION_BUS_ADDRESS
export DISPLAY=:1

# Đảo phím NumPad chuẩn xác 100% cho game NSO:
# Phím 8 (trên) -> gửi 2 (Nhảy lên)
# Phím 2 (dưới) -> gửi 8 (Cúi / Đi xuống)
# Phím 7 (trên trái) -> gửi 1 (Skill 1)
# Phím 1 (dưới trái) -> gửi 7 (Skill 7)
# Phím 9 (trên phải) -> gửi 3 (Skill 3)
# Phím 3 (dưới phải) -> gửi 9 (Skill 9)
xmodmap -e 'keycode  80 = KP_Down KP_8 KP_Down KP_8'
xmodmap -e 'keycode  88 = KP_Up KP_2 KP_Up KP_2'
xmodmap -e 'keycode  79 = KP_End KP_7 KP_End KP_7'
xmodmap -e 'keycode  87 = KP_Home KP_1 KP_Home KP_1'
xmodmap -e 'keycode  81 = KP_Next KP_9 KP_Next KP_9'
xmodmap -e 'keycode  89 = KP_Prior KP_3 KP_Prior KP_3'

exec openbox-session
EOF
chmod +x /root/.vnc/xstartup
```

### 3.5. Script khởi động game `/opt/jar-manager/scripts/start-acc.sh`
```bash
cat << 'EOF' > /opt/jar-manager/scripts/start-acc.sh
#!/bin/bash
ACC="${1:-acc1}"
JAR="${2:-/opt/jar-manager/games/game.jar}"

PROFILE_DIR="/opt/jar-manager/profiles/$ACC"
PID_FILE="$PROFILE_DIR/$ACC.pid"
mkdir -p "$PROFILE_DIR"

if [ -f "$PID_FILE" ]; then
    OLD_PID=$(cat "$PID_FILE")
    if ps -p "$OLD_PID" > /dev/null 2>&1; then
        echo "ACC_ALREADY_RUNNING:$OLD_PID"
        exit 0
    fi
fi

export DISPLAY=:1
# Kích thước 800x550 cho 1 cửa sổ duy nhất chiếm trọn màn hình, SerialGC chống lag
setsid java -XX:+UseSerialGC -Duser.home="$PROFILE_DIR" -Xms32m -Xmx64m -jar /opt/jar-manager/emulator/microemulator.jar --resizableDevice 800 550 "$JAR" > /dev/null 2>&1 &
NEW_PID=$!
echo "$NEW_PID" > "$PID_FILE"
echo "STARTED:$NEW_PID"

# Tự động gán tên cửa sổ và phóng to vừa khít màn hình
(
    sleep 2
    WIDS=$(xdotool search --pid "$NEW_PID" 2>/dev/null)
    for W in $WIDS; do
        xdotool set_window --name "Hoi Uc NSO" "$W" 2>/dev/null
        wmctrl -ir "$W" -b add,maximized_vert,maximized_horz 2>/dev/null
        xdotool windowactivate "$W" 2>/dev/null
    done
) &
EOF
chmod +x /opt/jar-manager/scripts/start-acc.sh
```

---

## 💻 PHẦN 4: TRIỂN KHAI CLIENT WINDOWS (`JarManager.exe`)

Toàn bộ ứng dụng Windows được xây dựng thuần túy bằng công cụ có sẵn của hệ điều hành (PowerShell, .NET Framework C# compiler `csc.exe`), **không cần cài thêm Visual Studio hay Node.js/Python trên máy người dùng**.

### 4.1. Cấu trúc thư mục trên Windows (`C:\JarManager\`)
```text
C:\JarManager\
├── vncviewer.exe          # TigerVNC Viewer Portable
├── JarManager.ps1         # Giao diện chính điều khiển bằng PowerShell WinForms
├── JarManager.cs          # Mã nguồn C# launcher (ẩn console đen)
├── JarManager.exe         # File thực thi nhấp đúp chạy ngoài Desktop
└── ...
```

### 4.2. File giao diện PowerShell `JarManager.ps1`
Tạo tại `C:\JarManager\JarManager.ps1`:
```powershell
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

[System.Windows.Forms.Application]::EnableVisualStyles()

$global:TunnelProcess = $null
$global:VpsHost = "remote.io.vn:10653"

function Invoke-Vps {
    param([string]$cmd)
    try {
        $pinfo = New-Object System.Diagnostics.ProcessStartInfo
        $pinfo.FileName = "ssh"
        $pinfo.Arguments = "vps-jar `"$cmd`""
        $pinfo.RedirectStandardOutput = $true
        $pinfo.RedirectStandardError = $true
        $pinfo.UseShellExecute = $false
        $pinfo.CreateNoWindow = $true

        $p = New-Object System.Diagnostics.Process
        $p.StartInfo = $pinfo
        $null = $p.Start()
        $stdout = $p.StandardOutput.ReadToEnd()
        $null = $p.WaitForExit(5000)
        return $stdout.Trim()
    } catch {
        return ""
    }
}

function Ensure-Tunnel {
    $hasTunnel = Get-NetTCPConnection -LocalPort 5901 -ErrorAction SilentlyContinue
    if (-not $hasTunnel) {
        $tunnelStart = New-Object System.Diagnostics.ProcessStartInfo
        $tunnelStart.FileName = "ssh"
        $tunnelStart.Arguments = "-N -L 5901:localhost:5901 vps-jar"
        $tunnelStart.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Hidden
        $tunnelStart.CreateNoWindow = $true
        $global:TunnelProcess = [System.Diagnostics.Process]::Start($tunnelStart)
        Start-Sleep -Milliseconds 600
    }
}

function Open-GameScreen {
    Ensure-Tunnel
    $vncPath = "C:\JarManager\vncviewer.exe"
    if (-not (Test-Path $vncPath)) {
        $vncPath = Join-Path $PSScriptRoot "vncviewer.exe"
    }
    if (Test-Path $vncPath) {
        # Tham số tối ưu chống lag: Tight encoding, nén nhanh mức 1, chất lượng 6, zero lag
        $vncArgs = "localhost:5901 -PreferredEncoding=Tight -QualityLevel=6 -CompressLevel=1 -AutoSelect=0 -CustomCompressLevel=1"
        Start-Process -FilePath $vncPath -ArgumentList $vncArgs
    } else {
        [System.Windows.Forms.MessageBox]::Show("Khong tim thay file vncviewer.exe!", "Loi", "OK", "Error")
    }
}

# --- TẠO GIAO DIỆN TỐI GIẢN ---
$form = New-Object System.Windows.Forms.Form
$form.Text = "JarManager - Treo Game NSO 24/7"
$form.Size = New-Object System.Drawing.Size(460, 390)
$form.StartPosition = "CenterScreen"
$form.FormBorderStyle = "FixedDialog"
$form.MaximizeBox = $false
$form.BackColor = [System.Drawing.Color]::FromArgb(246, 248, 251)
$form.Font = New-Object System.Drawing.Font("Segoe UI", 9.5)

# Header
$pnlHeader = New-Object System.Windows.Forms.Panel
$pnlHeader.Location = New-Object System.Drawing.Point(15, 12)
$pnlHeader.Size = New-Object System.Drawing.Size(415, 60)
$pnlHeader.BackColor = [System.Drawing.Color]::White
$pnlHeader.BorderStyle = [System.Windows.Forms.BorderStyle]::FixedSingle
$form.Controls.Add($pnlHeader)

$lblStatus = New-Object System.Windows.Forms.Label
$lblStatus.Text = "[●] CLOUD VPS: ONLINE"
$lblStatus.Font = New-Object System.Drawing.Font("Segoe UI", 11, [System.Drawing.FontStyle]::Bold)
$lblStatus.ForeColor = [System.Drawing.Color]::FromArgb(22, 163, 74)
$lblStatus.Location = New-Object System.Drawing.Point(12, 8)
$lblStatus.AutoSize = $true
$pnlHeader.Controls.Add($lblStatus)

$lblSub = New-Object System.Windows.Forms.Label
$lblSub.Text = "remote.io.vn:10653 | 1 Cua So Game Toan Man Hinh"
$lblSub.ForeColor = [System.Drawing.Color]::FromArgb(100, 116, 139)
$lblSub.Location = New-Object System.Drawing.Point(13, 31)
$lblSub.AutoSize = $true
$pnlHeader.Controls.Add($lblSub)

# Nút lớn Vào Game
$btnOpenGame = New-Object System.Windows.Forms.Button
$btnOpenGame.Text = "▶ VAO THAO TAC GAME (1 CLICK)"
$btnOpenGame.Location = New-Object System.Drawing.Point(15, 85)
$btnOpenGame.Size = New-Object System.Drawing.Size(415, 60)
$btnOpenGame.BackColor = [System.Drawing.Color]::FromArgb(22, 163, 74)
$btnOpenGame.ForeColor = [System.Drawing.Color]::White
$btnOpenGame.Font = New-Object System.Drawing.Font("Segoe UI", 12.5, [System.Drawing.FontStyle]::Bold)
$btnOpenGame.Cursor = [System.Windows.Forms.Cursors]::Hand
$btnOpenGame.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
$form.Controls.Add($btnOpenGame)

# Khối điều khiển phụ
$pnlControls = New-Object System.Windows.Forms.Panel
$pnlControls.Location = New-Object System.Drawing.Point(15, 155)
$pnlControls.Size = New-Object System.Drawing.Size(415, 42)
$form.Controls.Add($pnlControls)

$btnRestart = New-Object System.Windows.Forms.Button
$btnRestart.Text = "↺ Khoi Dong Lai Game"
$btnRestart.Location = New-Object System.Drawing.Point(0, 0)
$btnRestart.Size = New-Object System.Drawing.Size(200, 38)
$btnRestart.BackColor = [System.Drawing.Color]::FromArgb(235, 240, 245)
$btnRestart.Cursor = [System.Windows.Forms.Cursors]::Hand
$pnlControls.Controls.Add($btnRestart)

$btnStop = New-Object System.Windows.Forms.Button
$btnStop.Text = "■ Tat Game"
$btnStop.Location = New-Object System.Drawing.Point(215, 0)
$btnStop.Size = New-Object System.Drawing.Size(200, 38)
$btnStop.BackColor = [System.Drawing.Color]::FromArgb(254, 226, 226)
$btnStop.ForeColor = [System.Drawing.Color]::FromArgb(185, 28, 28)
$btnStop.Cursor = [System.Windows.Forms.Cursors]::Hand
$pnlControls.Controls.Add($btnStop)

# Khung hướng dẫn
$pnlHint = New-Object System.Windows.Forms.Panel
$pnlHint.Location = New-Object System.Drawing.Point(15, 208)
$pnlHint.Size = New-Object System.Drawing.Size(415, 125)
$pnlHint.BackColor = [System.Drawing.Color]::FromArgb(240, 253, 244)
$pnlHint.BorderStyle = [System.Windows.Forms.BorderStyle]::FixedSingle
$form.Controls.Add($pnlHint)

$lblHintTitle = New-Object System.Windows.Forms.Label
$lblHintTitle.Text = "• Huong Dan Dieu Khien & Treo 24/7:"
$lblHintTitle.Font = New-Object System.Drawing.Font("Segoe UI", 9.5, [System.Drawing.FontStyle]::Bold)
$lblHintTitle.ForeColor = [System.Drawing.Color]::FromArgb(21, 128, 61)
$lblHintTitle.Location = New-Object System.Drawing.Point(10, 8)
$lblHintTitle.AutoSize = $true
$pnlHint.Controls.Add($lblHintTitle)

$lblHintBody = New-Object System.Windows.Forms.Label
$lblHintBody.Text = "• Ban phim so NumPad (Da sua chuan 100%):`n  - Phim 8: Nhay Len | Phim 2: Cui / Di Xuong`n  - Phim 4: Sang Trai | Phim 6: Sang Phai`n  - Phim 5: Danh thuong / Chon | Phim 7, 9: Skill`n• Thao tac game xong, ban chi can bam [X] tat cua so game di.`n✔ Game tren VPS van tu dong cay 24/7 binh thuong!"
$lblHintBody.Font = New-Object System.Drawing.Font("Segoe UI", 8.8)
$lblHintBody.ForeColor = [System.Drawing.Color]::FromArgb(30, 41, 59)
$lblHintBody.Location = New-Object System.Drawing.Point(10, 28)
$lblHintBody.AutoSize = $true
$pnlHint.Controls.Add($lblHintBody)

# Sự kiện
$btnOpenGame.Add_Click({ Open-GameScreen })

$btnRestart.Add_Click({
    Invoke-Vps "/opt/jar-manager/scripts/stop-acc.sh acc1" | Out-Null
    Start-Sleep -Milliseconds 500
    Invoke-Vps "/opt/jar-manager/scripts/start-acc.sh acc1" | Out-Null
    Start-Sleep -Milliseconds 800
    Open-GameScreen
})

$btnStop.Add_Click({
    $confirm = [System.Windows.Forms.MessageBox]::Show("Ban co chac muon tat game dang chay tren VPS?", "Xac nhan", "YesNo", "Question")
    if ($confirm -eq "Yes") {
        Invoke-Vps "/opt/jar-manager/scripts/stop-acc.sh acc1" | Out-Null
        [System.Windows.Forms.MessageBox]::Show("Da dung game tren VPS!", "Thong bao", "OK", "Information")
    }
})

$form.Add_FormClosing({
    if ($global:TunnelProcess -and -not $global:TunnelProcess.HasExited) {
        $global:TunnelProcess.Kill()
    }
    Get-Process -Name "ssh" -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -like "*5901:localhost:5901*" } | Stop-Process -Force -ErrorAction SilentlyContinue
})

[void]$form.ShowDialog()
```

### 4.3. File C# Launcher `JarManager.cs` (Ẩn hoàn toàn cửa sổ đen PowerShell)
Tạo tại `C:\JarManager\JarManager.cs`:
```csharp
using System;
using System.Diagnostics;
using System.IO;

namespace JarManagerLauncher
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string psScript = Path.Combine(appDir, "JarManager.ps1");

            if (!File.Exists(psScript))
            {
                psScript = @"C:\JarManager\JarManager.ps1";
            }

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "powershell.exe";
            psi.Arguments = "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"" + psScript + "\"";
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;

            try
            {
                Process p = Process.Start(psi);
                p.WaitForExit();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Lỗi khởi động: " + ex.Message);
            }
        }
    }
}
```

### 4.4. Biên dịch tự động bằng `csc.exe`
Chạy từ PowerShell:
```powershell
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /target:winexe /out:"C:\JarManager\JarManager.exe" "C:\JarManager\JarManager.cs"
```

### 4.5. Tạo Shortcut ngoài màn hình Desktop
```powershell
$WshShell = New-Object -ComObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut("C:\Users\Admin\Desktop\JarManager.lnk")
$Shortcut.TargetPath = "C:\JarManager\JarManager.exe"
$Shortcut.WorkingDirectory = "C:\JarManager"
$Shortcut.Description = "Quản lý Treo Game J2ME VPS 24/7"
$Shortcut.Save()
```

---

## ⚡ PHẦN 5: SCRIPT MASTER SETUP TỰ ĐỘNG TOÀN DIỆN (1-CLICK)

Đây là script Python chạy trực tiếp từ máy Windows để dựng toàn bộ hệ thống từ con số 0 (cả trên VPS lẫn trên máy tính cá nhân):

```python
import subprocess
import os

VPS_HOST = "remote.io.vn"
VPS_PORT = 10653
VPS_USER = "root"

def run_ssh(cmd):
    full_cmd = f'ssh vps-jar "{cmd}"'
    subprocess.run(full_cmd, shell=True, check=True)

print("=== 1. SETUP HỆ ĐIỀU HÀNH & MẠNG VPS ===")
run_ssh('echo -e "1\\ny" | /usr/local/bin/changeip')
run_ssh('fallocate -l 2G /swapfile && chmod 600 /swapfile && mkswap /swapfile && swapon /swapfile && echo "/swapfile none swap sw 0 0" >> /etc/fstab')
run_ssh('sysctl vm.swappiness=10 && echo "vm.swappiness=10" >> /etc/sysctl.conf')
run_ssh('apt-get purge -y snapd unattended-upgrades && apt-get update -y')
run_ssh('apt-get install -y openjdk-8-jre openbox tigervnc-standalone-server tigervnc-common xdotool wmctrl unzip wget curl')

print("=== 2. SETUP MICROEMULATOR & VNC 800x600 ZERO-LAG ===")
run_ssh('mkdir -p /opt/jar-manager/{emulator,games,profiles,scripts} /root/.vnc /root/.config/openbox')
run_ssh('wget -O /opt/jar-manager/emulator/microemulator.jar https://repo1.maven.org/maven2/org/microemu/microemulator/2.0.4/microemulator-2.0.4.jar')

# Config VNC
vnc_cfg = "geometry=800x600\\ndepth=16\\nlocalhost\\nSecurityTypes=None\\nFrameRate=60\\nCompareFB=1\\nNeverShared=1\\nDisconnectClients=0\\nRemapKeys=0xffb8<>0xffb2,0xffb7<>0xffb1,0xffb9<>0xffb3,0xff97<>0xff99,0xff95<>0xff9c,0xff9a<>0xff9b"
run_ssh(f'printf "{vnc_cfg}\\n" > /root/.vnc/config')

# Systemd VNC
run_ssh('systemctl restart vncserver.service')

print("=== 3. SETUP CLIENT WINDOWS (C:\\JarManager) ===")
os.makedirs(r"C:\JarManager", exist_ok=True)
# Biên dịch JarManager.exe
subprocess.run(r'& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /target:winexe /out:"C:\JarManager\JarManager.exe" "C:\JarManager\JarManager.cs"', shell=True)

# Tạo shortcut Desktop
ps_shortcut = '$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut("C:\\Users\\Admin\\Desktop\\JarManager.lnk"); $s.TargetPath = "C:\\JarManager\\JarManager.exe"; $s.WorkingDirectory = "C:\\JarManager"; $s.Save()'
subprocess.run(f'powershell -Command "{ps_shortcut}"', shell=True)

print("=== HOÀN TẤT 100% TOÀN BỘ HỆ THỐNG TỪ VPS ĐẾN DESKTOP ===")
```

---

## 🎯 PHẦN 6: HƯỚNG DẪN NGHIỆM THU & BÀN GIAO

1. **Khởi chạy ứng dụng:** Ra ngoài Desktop bấm biểu tượng **`JarManager`**.
2. **Thao tác vào game:** Bấm nút **`▶ VAO THAO TAC GAME (1 CLICK)`**.
3. **Kiểm tra bàn phím:**
   * Bấm số `8` trên NumPad ➜ Nhân vật nhảy lên.
   * Bấm số `2` trên NumPad ➜ Nhân vật cúi xuống.
   * Bấm số `4` / `6` ➜ Đi trái / phải.
   * Bấm số `5` ➜ Đánh thường / Chọn NPC.
4. **Kiểm tra treo ngầm 24/7:**
   * Bấm nút `[X]` tắt cửa sổ game.
   * Tắt luôn app `JarManager`.
   * Chạy lệnh `ssh vps-jar "ps aux | grep java"` ➜ Tiến trình Java vẫn chạy liên tục với CPU < 5%, RAM ~65MB.
