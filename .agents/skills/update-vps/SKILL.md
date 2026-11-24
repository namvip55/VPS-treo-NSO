---
name: update-vps
description: >-
  Cập nhật thông số hoặc di chuyển sang VPS mới trong danh sách: update-vps <name-vps> <thông số>.
  Kích hoạt khi người dùng muốn cập nhật thông số Host, Port NAT, IP mới của 1 VPS, chuyển đổi VPS hoặc thay thế máy chủ trong hệ thống.
---

# UPDATE VPS SKILL - CẬP NHẬT THÔNG SỐ & DI CHUYỂN VPS (TRÊN 1 MÁY CHỦ CHỈ ĐỊNH)

Skill này cung cấp quy trình và cú pháp chuẩn để **cập nhật thông số kết nối (Host, Port NAT, Key, Tên miền...) hoặc di chuyển hệ thống sang một máy chủ VPS mới** cho đúng 1 VPS cụ thể trong danh bạ hệ thống (`vps_registry.json` / `servers/<name-vps>/`).

---

## 📌 1. CÚ PHÁP CHUẨN: `update-vps <name-vps> <thông số>`

Mọi thao tác cập nhật VPS **BẮT BUỘC** gắn liền với tên/ID của VPS đích:

```powershell
# Cú pháp tổng quát:
python tools/vps_manager.py update-vps <name-vps> <host:port> [options]

# Ví dụ 1: Cập nhật Host và Port NAT mới cho vps1:
python tools/vps_manager.py update-vps vps1 14.250.250.229:12724 --sync

# Ví dụ 2: Cập nhật thông số và tên miền Web Phone riêng:
python tools/vps_manager.py update-vps vps2_sample 14.236.172.236:10892 --web-url "https://vps2.ngrok-free.dev" --sync
```

**Các thông số hỗ trợ:**
* `<name-vps>`: Tên/ID máy chủ trong danh bạ (ví dụ `vps1`, `vps2`, `khách Tuấn`...).
* `<host:port>`: Địa chỉ IPv4 trực tiếp và Cổng NAT mới (ví dụ: `14.250.250.229:12724`).
* `--name <tên>`: Tên gợi nhớ hiển thị trên Dashboard.
* `--web-url <url>`: Đường dẫn Web Mobile riêng cho VPS đó.
* `--sync`: Tự động đồng bộ các tập lệnh quản lý mới nhất sang VPS mới.

---

## ⚡ 2. NGUYÊN TẮC VẬN HÀNH & ĐIỀU KHIỂN

1. **Cấu hình Web Mobile Phone là BẮT BUỘC:**
   Sau khi cập nhật thông số hoặc dựng mới VPS, hệ thống **bắt buộc cấu hình nhánh Web Phone** để người dùng điều khiển trực tiếp trên Safari / Chrome (noVNC + Virtual Gamepad + mã PIN):
   ```powershell
   python tools/vps_manager.py setup-web <name-vps> --pin 1234
   ```
2. **KHÔNG TỰ Ý ĐÓNG GÓI CLIENT (EXE/ZIP):**
   * Nếu người dùng **không yêu cầu skill `package-client`**, Agent **TUYỆT ĐỐI KHÔNG TỰ Ý ĐÓNG GÓI** file EXE hay ZIP giao cho khách.
   * Giao diện điều khiển mặc định luôn là Web Phone. Chỉ khi người dùng yêu cầu rõ ràng (*"đóng gói app cho khách", "xuất file exe", "giao client PC"* hoặc gọi `package-client`) thì mới xuất file.
3. **Cô Lập Độc Lập:**
   * Thông số mới được ghi trực tiếp vào `servers/<name-vps>/config.json` và `vps_registry.json`.
   * Hoàn toàn không đụng chạm đến bất kỳ VPS nào khác trong danh bạ.

---

## ⚡ 2. Quy Trình Di Chuyển Sang VPS Mới 5 Bước (Hoàn Thành Trong 1 Phút)

### Bước 1: Kích hoạt mạng Internet trên VPS mới
* Đối với VPS IP NAT, sau khi nhà mạng tạo xong hoặc vừa Reinstall OS, VPS **chưa có internet**.
* Mở **noVNC Console** trên giao diện web quản lý VPS (hoặc SSH bằng mật khẩu), gõ:
  ```bash
  echo -e "1\ny" | /usr/local/bin/changeip
  ```
  *(Lệnh này cấp IP nội bộ và thiết lập bảng định tuyến VLAN `ssh_rt` để thông mạng).*

---

### Bước 2: Cập nhật SSH Alias `vps-jar` trên máy tính cá nhân
Mở file `C:\Users\Admin\.ssh\config` trên máy tính của bạn, sửa lại HostName và Port mới:

> [!WARNING]
> **CẢNH BÁO ROUND-ROBIN DNS CỦA `remote.io.vn`:**
> Tên miền `remote.io.vn` trả về nhiều địa chỉ IP xoay vòng (Round-Robin). Nếu đặt `HostName remote.io.vn`, SSH sẽ kết nối ngẫu nhiên vào các node khác nhau gây ra lỗi `Connection timed out` hoặc `Host key verification failed`.
> **BẮT BUỘC:** Thay `<HOST_HOAC_IP_MOI>` bằng **địa chỉ IPv4 thực tế** của máy chủ (ví dụ: `14.250.250.229`).

```ssh-config
Host vps-jar
    HostName <IPv4_THUC_TE>
    Port <PORT_NAT_MOI>
    User root
    IdentityFile ~/.ssh/id_ed25519_jar
    ServerAliveInterval 30
    ServerAliveCountMax 3
    StrictHostKeyChecking no
    UserKnownHostsFile /dev/null
```

---

### Bước 3: Nạp SSH Key Ed25519 vào VPS mới (Đăng nhập không pass)
Chạy script Python hoặc lệnh sau từ máy tính Windows để nạp khóa bảo mật:

```powershell
# Nạp public key của bạn vào VPS mới (nhập mật khẩu VPS mới 1 lần duy nhất)
type C:\Users\Admin\.ssh\id_ed25519_jar.pub | ssh -p <PORT_NAT_MOI> root@<HOST_MOI> "mkdir -p /root/.ssh && cat >> /root/.ssh/authorized_keys && chmod 700 /root/.ssh && chmod 600 /root/.ssh/authorized_keys"
```
*Sau bước này, gõ `ssh vps-jar` sẽ vào thẳng VPS mà không cần gõ mật khẩu.*

---

### Bước 4: Chạy Script Tự Động Setup Toàn Diện (Tự Động Chọn Profile Thấp Nhất Cho VPS Cùi 1:1)

> [!IMPORTANT]
> **QUY TẮC AUTO-TUNING CHO VPS 1:1 HOẶC RAM KHẢ DỤNG THẤP:**
> Script tự động kiểm tra RAM khả dụng (`free -m`). Nếu RAM $\le$ 1024MB (VPS cấu hình 1:1) hoặc RAM khả dụng $\le$ 600MB (VPS đang gánh Web, Bot, API, Server,...), hệ thống sẽ tự động kích hoạt **Profile thấp nhất (Ultra-Low Spec)**:
> - Giới hạn RAM Java Client: `-Xms16m -Xmx32m -XX:+UseSerialGC -XX:TieredStopAtLevel=1`.
> - Giảm tốc độ khung hình VNC xuống `FrameRate=15` hoặc `20` để giảm tải cho CPU 1 core.
> - Kích hoạt ngay Swap 2GB.
> 
> [!CAUTION]
> **NGUYÊN TẮC CÔ LẬP KHÔNG XÂM PHẠM:** Dự án chỉ làm tròn trách nhiệm treo game J2ME trong `/opt/jar-manager/`. Tuyệt đối không can thiệp, tắt, hay sửa đổi bất kỳ dịch vụ nào khác đang chạy trên VPS (Web Apache/Nginx, API tool, Bot,...).

Chạy script Python tự động hóa sau từ máy tính Windows để dựng lại 100% môi trường trong 60 giây:

```python
import subprocess
import time

def run(cmd):
    print(f">> {cmd}")
    subprocess.run(f'ssh vps-jar "{cmd}"', shell=True, check=True)

print("BẮT ĐẦU SETUP VPS MỚI...")

# 1. Tạo 2GB Swap & tối ưu swappiness=10
run("fallocate -l 2G /swapfile && chmod 600 /swapfile && mkswap /swapfile && swapon /swapfile && echo '/swapfile none swap sw 0 0' >> /etc/fstab")
run("sysctl vm.swappiness=10 && echo 'vm.swappiness=10' >> /etc/sysctl.conf")

# 2. Gỡ bỏ rác snapd & cài gói nền
# LƯU Ý: Nếu VPS đã có OpenJDK 17 (chạy Server NSO), MicroEmulator 2.0.4 chạy trực tiếp trên Java 17.
# TUYỆT ĐỐI KHÔNG cài openjdk-8-jre trên VPS có Server NSO để tránh đổi default java làm crash Server.
run("apt-get purge -y snapd unattended-upgrades && apt-get update -y")
run("apt-get install -y openbox tigervnc-standalone-server tigervnc-common xdotool wmctrl unzip wget curl")
# Chỉ cài openjdk-8-jre nếu VPS hoàn toàn sạch và chưa có bất kỳ phiên bản Java nào:
# run("apt-get install -y openjdk-8-jre")

# 3. Tạo cấu trúc thư mục
run("mkdir -p /opt/jar-manager/{emulator,games,profiles,scripts} /root/.vnc /root/.config/openbox")

# 4. Tải MicroEmulator
run("wget -O /opt/jar-manager/emulator/microemulator.jar https://repo1.maven.org/maven2/org/microemu/microemulator/2.0.4/microemulator-2.0.4.jar")

# 5. Cấu hình TigerVNC chuẩn 800x600, FrameRate=30 và Remap phím NumPad
vnc_conf = """geometry=800x600
depth=16
localhost
SecurityTypes=None
FrameRate=30
CompareFB=1
RemapKeys=0xffb8<>0xffb2,0xffb7<>0xffb1,0xffb9<>0xffb3,0xff97<>0xff99,0xff95<>0xff9c,0xff9a<>0xff9b"""
subprocess.run(f'ssh vps-jar "cat << \'EOF\' > /root/.vnc/config\\n{vnc_conf}\\nEOF"', shell=True)

vnc_service = """[Unit]
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
WantedBy=multi-user.target"""
subprocess.run(f'ssh vps-jar "cat << \'EOF\' > /etc/systemd/system/vncserver.service\\n{vnc_service}\\nEOF"', shell=True)
run("systemctl daemon-reload && systemctl enable vncserver.service")

# 6. Cấu hình xstartup đảo phím NumPad chuẩn
xstartup = """#!/bin/sh
unset SESSION_MANAGER
unset DBUS_SESSION_BUS_ADDRESS
export DISPLAY=:1

xmodmap -e 'keycode  80 = KP_Down KP_8 KP_Down KP_8'
xmodmap -e 'keycode  88 = KP_Up KP_2 KP_Up KP_2'
xmodmap -e 'keycode  79 = KP_End KP_7 KP_End KP_7'
xmodmap -e 'keycode  87 = KP_Home KP_1 KP_Home KP_1'
xmodmap -e 'keycode  81 = KP_Next KP_9 KP_Next KP_9'
xmodmap -e 'keycode  89 = KP_Prior KP_3 KP_Prior KP_3'

exec openbox-session"""
subprocess.run(f'ssh vps-jar "cat << \'EOF\' > /root/.vnc/xstartup\\n{xstartup}\\nEOF && chmod +x /root/.vnc/xstartup"', shell=True)

# 7. Khởi động VNC
run("systemctl restart vncserver.service")
print("SETUP MÔI TRƯỜNG THÀNH CÔNG!")
```

---

### Bước 5: Đồng bộ file game & nạp vào app JarManager
1. **Chuyển file game sang VPS mới:**
   ```powershell
   scp D:\project\VPS\HoiUc220.jar vps-jar:/opt/jar-manager/games/
   ssh vps-jar "ln -sf /opt/jar-manager/games/HoiUc220.jar /opt/jar-manager/games/game.jar"
   ```
2. **Copy các script quản lý lên VPS:**
   ```powershell
   scp D:\project\VPS\scratch\start-acc.sh vps-jar:/opt/jar-manager/scripts/
   ssh vps-jar "chmod +x /opt/jar-manager/scripts/*.sh"
   ```
3. **Cập nhật host mới vào [JarManager.ps1](file:///D:/project/VPS/JarManager.ps1):**
   Sửa dòng `$global:VpsHost = "<HOST_MOI>:<PORT_MOI>"`.
   Biên dịch lại `JarManager.exe`:
   ```powershell
   & "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /target:winexe /out:"C:\JarManager\JarManager.exe" "C:\JarManager\JarManager.cs"
   ```
4. **Bật game:** Mở `JarManager.exe` ngoài Desktop ➜ Bấm nút **`▶ VAO THAO TAC GAME (1 CLICK)`** là xong!

---

## ⚠️ 3. Nhắc Nhở Quan Trọng Khi Sử Dụng VPS Mới
* **TUYỆT ĐỐI KHÔNG BẬT UFW:** Trên VPS NAT, bật UFW sẽ khóa luôn kết nối SSH.
* **Treo 24/7:** Thao tác xong chỉ cần bấm [X] tắt cửa sổ VNC trên Windows, game trên VPS mới vẫn chạy 24/7.
