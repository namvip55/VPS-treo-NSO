---
name: vps-jar-setup
description: >-
  Cẩm nang và quy trình tự động setup toàn diện hệ thống VPS Linux (IP NAT) treo game J2ME (.jar / Ninja School Online) 24/7.
  Kích hoạt khi người dùng yêu cầu setup VPS mới, cài đặt lại VPS, kiểm tra thông tin VPS, kết nối SSH, VNC, hoặc quản lý tài khoản game.
---

# MASTER SKILL: SETUP TOÀN DIỆN VPS J2ME (.JAR) & TRIỂN KHAI CLIENT ĐIỀU KHIỂN (FORM CHUẨN)

Skill này là **khung mẫu thiết kế chuẩn (Reference Blueprint)** của toàn bộ dự án, hướng dẫn từ A đến Z: từ cấu hình hệ điều hành Linux trên VPS cấu hình thấp (1 vCPU, 1GB RAM, IP NAT), tối ưu hiển thị Zero-Lag 60 FPS NoJPEG, cho đến việc tự động đóng gói ứng dụng điều khiển 1-click **`ClientLauncher.exe`** trên máy tính Windows với bảo mật tối đa.

---

## 📋 PHẦN 1: BẢNG THÔNG SỐ FORM MẪU (CHUẨN DỰ ÁN)

### 1.1. Thông Số Phần Cứng & Mạng VPS
* **Nhà cung cấp:** `vps1k.net` (hoặc bất kỳ VPS NAT nào)
* **Hệ điều hành:** Ubuntu 22.04 LTS x86_64 (Minimal)
* **Phần cứng:** 1 vCPU, 1024 - 2048 MB RAM, 15 GB NVMe (SCSI), 2 GB Swap (`/swapfile`), Tổng Swap ~3.8 GB
* **Băng thông:** 40 Mbps (5 MB/s)
* **Loại mạng:** IP NAT (Việt Nam)
* **Host Remote:** Cố định địa chỉ IPv4 trực tiếp (Ví dụ: `14.250.250.229`). TUYỆT ĐỐI không dùng domain `remote.io.vn` do DNS Round-Robin gây timeout.
* **Port NAT (SSH):** Port NAT nhà cung cấp cấp (Ví dụ: `12724` hoặc `10653`)
* **User quản trị:** `root` | **Mật khẩu:** `<ROOT_PASSWORD>`
* **SSH Key quản trị:** `~/.ssh/id_ed25519_jar` (Ed25519) | Alias: `vps-jar` trong `~/.ssh/config`
* **Lệnh cấp mạng bắt buộc khi cài mới:** `echo -e "1\ny" | /usr/local/bin/changeip`

---

## ⚠️ PHẦN 2: CÁC NGUYÊN TẮC SỐNG CÒN (CRITICAL RULES)

> [!CAUTION]
> **1. TUYỆT ĐỐI KHÔNG BẬT UFW (`ufw enable`):**
> Do VPS nằm sau router NAT định tuyến qua interface VLAN ảo `eth0.50` và bảng `ssh_rt`. Khi bật UFW, bộ lọc `rp_filter` của Linux coi gói tin phản hồi là giả mạo và drop 100% packets, gây mất kết nối SSH vĩnh viễn (buộc phải Reinstall OS).
> Router nhà mạng bên ngoài đã chặn 100% cổng, chỉ mở cổng SSH NAT đã ánh xạ. VNC chỉ bind `localhost`. Không cần và cấm bật UFW.

> [!IMPORTANT]
> **2. DÙNG IPV4 TRỰC TIẾP — TRÁNH ROUND-ROBIN DNS CỦA `remote.io.vn`:**
> Tên miền `remote.io.vn` luân phiên trỏ về nhiều cụm IP khác nhau (`14.250.250.229`, `14.236.172.236`,...). Luôn dùng IPv4 cố định trong cấu hình và scripts.

> [!WARNING]
> **3. KHÔNG CÀI `openjdk-8-jre` NẾU VPS CÓ SERVER NSO (JAVA 17):**
> MicroEmulator 2.0.4 chạy mượt mà trên **OpenJDK 17**. Nếu VPS đang chạy Server NSO (Java 17), cài thêm Java 8 sẽ làm đè `/usr/bin/java` gây lỗi `UnsupportedClassVersionError` làm sập server. Chỉ cài `openjdk-17-jre-headless` nếu VPS chưa có Java.

> [!IMPORTANT]
> **4. LỆNH NHẬN MẠNG `changeip` KHI CÀI MỚI VPS:**
> Khi cài lại Ubuntu từ web, card mạng chưa có internet. Luôn phải chạy:
> ```bash
> echo -e "1\ny" | /usr/local/bin/changeip
> ```

> [!TIP]
> **5. CƠ CHẾ CHẠY NỀN VĨNH VIỄN (`setsid`):**
> Mọi tiến trình Java phải bọc trong `setsid`:
> `setsid java ... > /dev/null 2>&1 &`
> Giúp game hoàn toàn tách biệt khỏi SSH/VNC; tắt app hay tắt máy tính thì game vẫn cày 24/7.

> [!NOTE]
> **6. CÔ LẬP DỮ LIỆU TÀI KHOẢN (RMS ISOLATION):**
> Luôn gán: `-Duser.home=/opt/jar-manager/profiles/<tên_acc>` để tránh đè mật khẩu và dữ liệu game.

> [!IMPORTANT]
> **7. TỰ ĐỘNG CHỌN PROFILE THẤP NHẤT CHO VPS 1:1 HOẶC RAM KHẢ DỤNG THẤP:**
> Khi phát hiện VPS chỉ có 1 vCPU và RAM $\le$ 1100MB (hoặc RAM khả dụng `available` $\le$ 600MB):
> - **Java Heap:** Tự động hạ `-Xms16m -Xmx32m -XX:TieredStopAtLevel=1`.
> - **TigerVNC:** Giữ `800x600 -depth 16` kèm `CompareFB=1`.
> - **Swap:** Đảm bảo có Swapfile 2GB (`vm.swappiness=10`) chống OOM Killer.

> [!CAUTION]
> **8. NGUYÊN TẮC CÔ LẬP TUYỆT ĐỐI & KHÔNG XÂM PHẠM DỰ ÁN KHÁC (STRICT NON-INTRUSION):**
> VPS có thể chạy chung với **Web, Tool API, Bot, Game Server,...** Dự án này chỉ làm đúng nhiệm vụ treo game trong `/opt/jar-manager/` và VNC Display `:1`. Tuyệt đối **KHÔNG can thiệp, tắt, gỡ bỏ hay đụng chạm** vào bất kỳ dịch vụ hoặc dữ liệu nào khác trên máy chủ.

> [!IMPORTANT]
> **9. MẶC ĐỊNH ĐIỀU KHIỂN QUA WEB PHONE (BẮT BUỘC) — KHÔNG TỰ Ý ĐÓNG GÓI CLIENT:**
> Sau khi cài đặt VPS xong, hệ thống **BẮT BUỘC cấu hình giao diện Web Phone** (`python tools/vps_manager.py setup-web <name-vps>`) để người dùng/khách điều khiển trực tiếp trên trình duyệt Safari/Chrome (noVNC + Virtual Gamepad + mã PIN).
> **Nếu người dùng không yêu cầu skill `package-client`**, Agent **TUYỆT ĐỐI KHÔNG TỰ Ý ĐÓNG GÓI** file `NSO_Client.exe` hay ZIP. Chỉ đóng gói khi có yêu cầu rõ ràng.

> [!TIP]
> **10. THAO TÁC LUÔN GẮN LIỀN VỚI 1 VPS CỤ THỂ:**
> Luôn chỉ định rõ tên hoặc ID của VPS trong danh bạ (`vps_registry.json`) trước khi thực thi bất kỳ thao tác nào (`vps1`, `vps2`, `khách_tuấn`...). Cấm thao tác chung chung.

---

## 🖥️ PHẦN 3: CẤU HÌNH SERVER VPS (ZERO-LAG 60 FPS ENGINE)

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
   # Đảm bảo có Java Runtime (OpenJDK 17 tương thích 100% cả Client lẫn Server)
   which java >/dev/null 2>&1 || apt-get install -y openjdk-17-jre-headless
   apt-get install -y openbox tigervnc-standalone-server tigervnc-common xdotool wmctrl unzip wget curl
   ```

### 3.2. Cấu trúc thư mục `/opt/jar-manager/`
```bash
mkdir -p /opt/jar-manager/{emulator,games,profiles,scripts} /root/.vnc /root/.config/openbox
wget -O /opt/jar-manager/emulator/microemulator.jar https://repo1.maven.org/maven2/org/microemu/microemulator/2.0.4/microemulator-2.0.4.jar
```

### 3.3. Cấu hình TigerVNC Server (800x600 -depth 16, 60 FPS & Remap Phím)
Tạo file `/root/.vnc/config`:
```ini
geometry=800x600
depth=16
localhost
SecurityTypes=None
FrameRate=60
CompareFB=1
RemapKeys=0xffb8<>0xffb2,0xffb7<>0xffb1,0xffb9<>0xffb3,0xff97<>0xff99,0xff95<>0xff9c,0xff9a<>0xff9b
```

Tạo service systemd `/etc/systemd/system/vncserver.service`:
```ini
[Unit]
Description=TigerVNC Server on Display :1 (Optimized 800x600 60FPS)
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

# Đảo phím NumPad chuẩn xác 100% cho game NSO
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

### 3.5. Đồng bộ tập lệnh vận hành (`/opt/jar-manager/scripts/`)
Các script chính:
- `start-acc.sh [acc] [jar]`: Tự động nhận diện RAM, tự động lấy tên game từ MANIFEST.MF, khởi chạy ngầm 24/7.
- `stop-acc.sh [acc]`: Dừng an toàn tài khoản game.
- `mode-active.sh [acc]`: Nâng `renice -n 0` và focus cửa sổ khi khách thao tác (0 lag).
- `mode-idle.sh [acc]`: Hạ `renice -n 15` đưa CPU về mức tối thiểu khi treo ngầm.
- `auto-power-save.sh`: Quét toàn bộ profiles và tự động chuyển active/idle dựa trên trạng thái kết nối VNC.
- `client-cmd-wrapper.sh`: Bảo vệ máy chủ, chỉ cho phép khóa SSH của khách duy trì tunnel VNC cổng 5901 và chạy script game, cấm hoàn toàn shell root.

---

## 💻 PHẦN 4: TRIỂN KHAI CLIENT ĐIỀU KHIỂN (WINDOWS & IPHONE)

### 4.1. Nhánh Windows: Client EXE 1-Click ([ClientLauncher.cs](file:///d:/project/Vps-Treo-Nso/tools/ClientLauncher.cs) & [package_client.py](file:///d:/project/Vps-Treo-Nso/tools/package_client.py))
Hệ thống sử dụng bộ mã nguồn C# thuần túy [ClientLauncher.cs](file:///d:/project/Vps-Treo-Nso/tools/ClientLauncher.cs), biên dịch bằng trình biên dịch `.NET 4.0` (`csc.exe`) có sẵn trên 100% máy Windows:
- **Tự động cấp Port nội bộ linh hoạt (5901–5950)**: Cho phép mở nhiều client quản lý nhiều VPS cùng lúc không bị xung đột.
- **Tự động phân quyền ACL cá nhân**: Tránh lỗi OpenSSH `UNPROTECTED PRIVATE KEY FILE`.
- **Bảo mật tuyệt đối**: Khóa SSH cấp cho khách là khóa riêng biệt (Ed25519), được cấu hình qua wrapper chỉ mở đúng cổng VNC 5901, không có quyền root.
- **Đóng gói 1-Click tự động**: Sử dụng [package_client.py](file:///d:/project/Vps-Treo-Nso/tools/package_client.py) để xuất file EXE duy nhất hoặc gói ZIP Portable giao cho khách.

### 4.2. Nhánh iPhone / iPad: Web Mobile PWA & Ngrok Permanent Tunnel ([web/](file:///d:/project/Vps-Treo-Nso/web/))
Đối với hệ điều hành iOS (không chạy được EXE và bị Apple tự động ngắt kết nối SSH ngầm):
- **Cơ chế Web-based noVNC**: Biến Display `:1` (Port 5901) thành WebSocket HTML5 Canvas trên cổng nội bộ 6080 qua `websockify`.
- **Cụm Phím Ảo Cảm Ứng (Virtual Touch D-Pad)**: Tích hợp sẵn cụm nút di chuyển và kỹ năng chuẩn Ninja School (`8: Nhảy`, `2: Cúi`, `4/6: Trái/Phải`, `5: Đánh`, `7/9/1/3: Skill`, `*, #, 0`). Nút bấm bán trong suốt, hỗ trợ đa điểm, rung phản hồi xúc giác nhẹ (Haptic Feedback) không che khuất tầm nhìn.
- **Cố Định Link Vĩnh Viễn Bằng Ngrok Static Domain**:
  Sử dụng 1 domain tĩnh miễn phí từ tài khoản Ngrok (ví dụ: `https://namnso.ngrok-free.app`), chạy làm service nền `ngrok.service` 24/7.
  - Link không bao giờ thay đổi dù VPS khởi động lại bao nhiêu lần.
  - Khách chỉ cần mở Safari $\rightarrow$ Bấm nút **Chia Sẻ** $\rightarrow$ Chọn **Thêm vào MH chính (Add to Home Screen)** để biến thành 1 App Game toàn màn hình.
- **Lệnh Cài Đặt 1-Click Trên VPS**:
  ```bash
  bash /opt/jar-manager/scripts/setup-ios-web.sh --token <NGROK_TOKEN> --domain <YOUR_DOMAIN.ngrok-free.app>
  ```


---

## ⚡ PHẦN 5: SCRIPT MASTER SETUP TỰ ĐỘNG TOÀN DIỆN (1-CLICK)

Script Python dựng toàn bộ hệ thống từ con số 0 (sử dụng `shell=False`, argument list an toàn chống injection):

```python
import subprocess
import os

VPS_HOST = "14.250.250.229"  # Điền IPv4 trực tiếp của VPS
VPS_PORT = 12724             # Điền Port NAT SSH
ADMIN_KEY = os.path.expanduser("~/.ssh/id_ed25519_jar")

def run_ssh(remote_cmd):
    cmd = [
        "ssh", "-p", str(VPS_PORT),
        "-o", "StrictHostKeyChecking=no",
        "-o", "UserKnownHostsFile=/dev/null",
        "-o", "ConnectTimeout=8"
    ]
    if os.path.exists(ADMIN_KEY):
        cmd.extend(["-i", ADMIN_KEY])
    cmd.extend([f"root@{VPS_HOST}", remote_cmd])
    subprocess.run(cmd, check=True)

print("=== 1. SETUP HỆ ĐIỀU HÀNH & MẠNG VPS ===")
run_ssh('echo -e "1\\ny" | /usr/local/bin/changeip')
run_ssh('fallocate -l 2G /swapfile && chmod 600 /swapfile && mkswap /swapfile && swapon /swapfile && echo "/swapfile none swap sw 0 0" >> /etc/fstab')
run_ssh('sysctl vm.swappiness=10 && echo "vm.swappiness=10" >> /etc/sysctl.conf')
run_ssh('apt-get purge -y snapd unattended-upgrades && apt-get update -y')
run_ssh('which java >/dev/null 2>&1 || apt-get install -y openjdk-17-jre-headless; apt-get install -y openbox tigervnc-standalone-server tigervnc-common xdotool wmctrl unzip wget curl')

print("=== 2. SETUP MICROEMULATOR & VNC 60 FPS ZERO-LAG ===")
run_ssh('mkdir -p /opt/jar-manager/{emulator,games,profiles,scripts} /root/.vnc /root/.config/openbox')
run_ssh('wget -O /opt/jar-manager/emulator/microemulator.jar https://repo1.maven.org/maven2/org/microemu/microemulator/2.0.4/microemulator-2.0.4.jar')

# Config VNC 60 FPS NoJPEG
vnc_cfg = "geometry=800x600\\ndepth=16\\nlocalhost\\nSecurityTypes=None\\nFrameRate=60\\nCompareFB=1\\nRemapKeys=0xffb8<>0xffb2,0xffb7<>0xffb1,0xffb9<>0xffb3,0xff97<>0xff99,0xff95<>0xff9c,0xff9a<>0xff9b"
run_ssh(f'printf "{vnc_cfg}\\n" > /root/.vnc/config')
run_ssh('systemctl restart vncserver.service')

print("=== 3. XUẤT FILE CLIENT ĐIỀU KHIỂN CHO KHÁCH ===")
subprocess.run([
    "python", "tools/package_client.py",
    "--host", VPS_HOST,
    "--port", str(VPS_PORT),
    "--title", "NSO GEMINI",
    "--acc", "acc1",
    "--push-pubkey"
], check=True)

print("=== HOÀN TẤT 100% TOÀN BỘ HỆ THỐNG ===")
```

---

## 🎯 PHẦN 6: HƯỚNG DẪN NGHIỆM THU & BÀN GIAO

1. **Khởi chạy ứng dụng:** Mở file EXE trong `dist_clients/` (ví dụ: `NSO_GEMINI_Client.exe`).
2. **Thao tác vào game:** Bấm nút **`▶ VAO THAO TAC GAME (1 CLICK)`**. Màn hình game hiển thị ngay với tốc độ 60 FPS cực mượt.
3. **Kiểm tra bàn phím:**
   * Bấm số `8` trên NumPad ➜ Nhân vật nhảy lên.
   * Bấm số `2` trên NumPad ➜ Nhân vật cúi xuống.
   * Bấm số `4` / `6` ➜ Đi trái / phải.
   * Bấm số `5` ➜ Đánh thường / Chọn NPC.
4. **Kiểm tra treo ngầm 24/7:**
   * Bấm nút `[X]` tắt cửa sổ game.
   * Tắt app điều khiển.
   * Chạy lệnh `ssh vps-jar "ps aux | grep java"` ➜ Tiến trình Java vẫn chạy liên tục với CPU < 5%, RAM ~65MB.
