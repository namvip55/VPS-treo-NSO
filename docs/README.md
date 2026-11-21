# HỆ THỐNG TREO GAME J2ME (.JAR) 24/7 TRÊN VPS LINUX & CLIENT WINDOWS

Hệ thống chuyên dụng tối ưu hóa tài nguyên phần cứng để treo game Java J2ME (Ninja School Online) liên tục 24/7 trên VPS Ubuntu cấu hình tiết kiệm (1 vCPU, 1GB RAM) với mức tiêu thụ RAM cực nhẹ (< 150MB nền), 1 cửa sổ duy nhất chiếm trọn màn hình và độ trễ thao tác gần như bằng 0 (Zero-Lag Engine).

---

## 📌 1. Thông Tin Môi Trường & Kết Nối Đầy Đủ

| Thành phần | Thông tin chi tiết |
| :--- | :--- |
| **Server VPS** | Ubuntu 22.04 LTS x86_64 (1 vCPU, 1GB RAM, 15GB NVMe, Băng thông 40Mbps) |
| **Loại mạng** | IP NAT Việt Nam (Định tuyến VLAN qua bảng `ssh_rt`, interface `eth0.50`) |
| **Host Remote** | `remote.io.vn` |
| **Cổng SSH (Port NAT)** | `10653` (Ánh xạ vào cổng SSH 22 nội bộ) |
| **User quản trị** | `root` |
| **Mật khẩu quản trị gốc** | `<ROOT_PASSWORD>` *(Đã nạp SSH Key Ed25519 không cần gõ mật khẩu)* |
| **SSH Key cá nhân** | `C:\Users\Admin\.ssh\id_ed25519_jar` (Alias: `vps-jar`) |
| **Lệnh nhận mạng** | `echo -e "1\ny" \| /usr/local/bin/changeip` (Bắt buộc chạy khi cài mới) |
| **Màn hình ảo (VNC)** | Display `:1` (Cổng nội bộ `5901`), độ phân giải chuẩn **800x600 -depth 16** |
| **Tốc độ khung hình VNC** | Cố định `FrameRate=60` + `CompareFB=1` + `-NoJPEG=1` (Hiển thị 60 FPS siêu mượt, nét từng pixel, giảm 50% CPU) |
| **Cơ chế điều tiết tài nguyên** | Dual-Mode: Active Mode (`renice 0` khi xem) / Idle Mode (`renice 15` khi treo ngầm 24/7) |
| **Cơ chế truy cập VNC** | SSH Tunneling qua `localhost:5901` (Bảo mật 100%, không mở port VNC ra internet) |
| **Khung vẽ game** | MicroEmulator 2.0.4 (`--resizableDevice 800 550`), 1 cửa sổ duy nhất toàn màn hình |
| **Bộ dọn rác Java** | `-XX:+UseSerialGC` (Loại bỏ luồng dọn rác nền, CPU giảm còn < 5%) |
| **File game mặc định** | Symlink `/opt/jar-manager/games/game.jar` (quản lý độc lập theo từng node trong `servers/`) |
| **Client App** | Ứng dụng độc lập biên dịch từ `tools/ClientLauncher.cs` (Đóng gói 1-click qua `tools/package_client.py`) |

---

## 🚀 2. Cách Sử Dụng Hàng Ngày (2 Cách)

### Cách 1: Dùng App Giao Diện Windows (Khuyên Dùng - 1 Click)
1. Ra ngoài **Desktop**, bấm đúp vào biểu tượng **`JarManager`** (hoặc mở file [JarManager.exe](file:///C:/JarManager/JarManager.exe) tại `C:\JarManager\`).
2. Bấm nút lớn duy nhất: **`🎮 VÀO THAO TÁC GAME (1 CLICK)`**.
3. Cửa sổ game mở ngay tức thì:
   * **1 cửa sổ game duy nhất:** Phủ kín màn hình, không chia tab, không có thanh taskbar thừa.
   * **Không bị lag:** CPU cực thấp (~4%), thao tác chuột và bàn phím mượt mà.
4. **Treo 24/7:** Thao tác game xong (đăng nhập, cài auto, chọn khu), bạn chỉ cần **bấm nút [X] tắt cửa sổ game đi** — VPS vẫn tự cày liên tục 24/7!

---

### Cách 2: Điều Khiển Bằng Dòng Lệnh (CLI / PowerShell)

Các lệnh đã được cấu hình sẵn alias `vps-jar` trong file `~/.ssh/config`:

| Thao tác | Lệnh PowerShell |
| :--- | :--- |
| **Mở SSH Tunnel màn hình** | `ssh -N -L 5901:localhost:5901 vps-jar` *(Sau đó mở vncviewer.exe kết nối `localhost:5901`)* |
| **Bật game** | `ssh vps-jar "/opt/jar-manager/scripts/start-acc.sh acc1"` |
| **Tắt game** | `ssh vps-jar "/opt/jar-manager/scripts/stop-acc.sh acc1"` |
| **Xem trạng thái game** | `ssh vps-jar "/opt/jar-manager/scripts/status-all.sh"` |
| **Kiểm tra RAM, CPU, Swap** | `ssh vps-jar "/opt/jar-manager/scripts/system-info.sh"` |

---

## 🎮 3. Phím Tắt Điều Khiển Game (Đã Chuẩn Hóa 100%)

Hệ thống đã can thiệp đảo phím 2 lớp (ở cả server TigerVNC `RemapKeys` và X11 `xmodmap`), khắc phục triệt để lỗi phím số 2 bị nhảy lên trên:

| Phím trên NumPad máy tính | Chức năng trong game Ninja School | Ghi chú |
| :--- | :--- | :--- |
| **Phím 8 (ở trên)** | **Nhảy Lên** (Gửi mã 2 vào game) | Bấm phím trên ➜ nhân vật nhảy lên |
| **Phím 2 (ở dưới)** | **Cúi / Đi Xuống** (Gửi mã 8 vào game) | Bấm phím dưới ➜ nhân vật cúi/xuống |
| **Phím 4** | **Sang Trái** | Di chuyển sang trái |
| **Phím 6** | **Sang Phải** | Di chuyển sang phải |
| **Phím 5** | **Đánh Thường / Chọn / Nói chuyện** | Phím trung tâm |
| **Phím 7 / 9** | **Skill 1 / Skill 3** | Kỹ năng hàng trên |
| **Phím 1 / 3** | **Skill 7 / Skill 9** | Kỹ năng hàng dưới |
| **Phím 0 / Space / Enter** | **Đánh thường / Chọn / Tương tác NPC** | Tương tác nhanh |
| **Phím mũi tên (↑ ↓ ← →)** | **Lên / Xuống / Trái / Phải** | Điều khiển di chuyển phụ |

---

## 📂 4. Danh Mục Tài Liệu Chi Tiết

* [Kiến Trúc Kỹ Thuật & Tối Ưu RAM (ARCHITECTURE.md)](ARCHITECTURE.md)
* [Sổ Tay Vận Hành & Quản Lý Tập Lệnh (OPERATIONS.md)](OPERATIONS.md)
* [Đặc Tả Cấu Hình Mạng NAT & Bài Học Xử Lý Sự Cố (NETWORK_AND_TROUBLESHOOTING.md)](NETWORK_AND_TROUBLESHOOTING.md)
* [Skill Tự Động Deploy VPS Trong 60s (.agents/skills/vps-jar-setup/SKILL.md)](../.agents/skills/vps-jar-setup/SKILL.md)
