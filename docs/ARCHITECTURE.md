# KIẾN TRÚC KỸ THUẬT HỆ THỐNG (TECHNICAL ARCHITECTURE)

Tài liệu này mô tả chi tiết toàn bộ thiết kế kiến trúc, các lớp công nghệ, cơ chế cô lập dữ liệu và tối ưu tài nguyên của hệ thống treo game J2ME.

---

## 🏗️ 1. Sơ Đồ Kiến Trúc Tổng Thể

```
+---------------------------------------------------------------------------------+
|                               CLIENT (WINDOWS 10/11)                            |
|                                                                                 |
|   +--------------------------+                +-----------------------------+   |
|   | JarManager.exe           |                |  TigerVNC Viewer Portable   |   |
|   | (Điều khiển 1-click)     |                |  (Hiển thị màn hình Game)   |   |
|   +------------+-------------+                +--------------+--------------+   |
|                |                                             |                  |
|                | SSH Command                                 | localhost:5901   |
|                v                                             v                  |
|   +-------------------------------------------------------------------------+   |
|   |                       OpenSSH Client (Key Ed25519)                      |   |
|   |            SSH Tunnel: -L 5901:localhost:5901 (Port NAT: 10653)         |   |
|   |            Tight Encoding + QualityLevel 6 + CompressLevel 2            |   |
+---|-------------------------------------------------------------------------|---+
    |                                 MÃ HÓA INTERNET                         |
+---|-------------------------------------------------------------------------|---+
    |   v                                                                     v   |
    |                           SERVER (VPS UBUNTU 22.04 LTS)                     |
    |                                                                             |
    |   +-------------------------------------------------------------------------+   |
    |   |                         OpenSSH Server (Port 22 nội bộ)                 |   |
    |   +--------------------+-------------------------------------+--------------+   |
    |                        |                                     |                  |
    |                        v                                     v                  |
    |   +---------------------------------------+   +-----------------------------+   |
    |   |       Scripts Quản lý (/opt/...)      |   |  TigerVNC Server (Port 5901)|   |
    |   |   start-acc | stop-acc | focus-acc    |   |  - Display :1 (800x600)     |   |
    |   +--------------------+------------------+   |  - Localhost only (-local)  |   |
    |                        |                      +--------------+--------------+   |
    |                        |                                     |                  |
    |                        v                                     v                  |
    |   +-------------------------------------------------------------------------+   |
    |   |                  Openbox Window Manager + tint2 Taskbar                 |   |
    |   |       - Cấu hình rc.xml: Tự động Toàn Màn Hình (<maximized>yes</>)      |   |
    |   |       - Thanh taskbar tint2: 1-click chuyển acc [acc1] [acc2]           |   |
    |   |       - Chuẩn hóa bàn phím bằng xmodmap (Đảo NumPad 7 8 9 <-> 1 2 3)    |   |
    |   +-------------------------------------------------------------------------+   |
    |                                        |                                        |
    |         +------------------------------+------------------------------+         |
    |         |                                                             |         |
    |         v                                                             v         |
    |   +--------------------+                                        +---------------+ |
    |   |  Java OpenJDK 8    |                                        |  Java OpenJDK | |
    |   |  MicroEmulator     |                                        |  MicroEmulator| |
    |   |  Account: acc1     |                                        |  Account: acc2| |
    |   |  RAM: -Xms32m -Xmx64m                                       |  RAM: -Xmx64m | |
    |   |  --resizableDevice 800 520                                  |  800x520      | |
    |   +---------+----------+                                        +-------+-------+ |
    |             |                                                           |         |
    |             v                                                           v         |
    |   +--------------------+                                        +---------------+ |
    |   | Profile RMS: acc1  |                                        | Profile RMS:  | |
    |   | /profiles/acc1/    |                                        | /profiles/acc2| |
    |   +--------------------+                                        +---------------+ |
    |                                                                                 |
    |   +-------------------------------------------------------------------------+   |
    |   |                   Swapfile 2GB (Swappiness = 10)                        |   |
    |   |           Chống tràn bộ nhớ OOM Killer khi mở nhiều tab Java            |   |
    |   +-------------------------------------------------------------------------+   |
    +---------------------------------------------------------------------------------+
```

---

## 🧠 2. Các Thành Phần Kỹ Thuật Trọng Tâm

### 2.1. Tối ưu Hệ Điều Hành (OS Layer)
* **Loại bỏ dịch vụ thừa:** Gỡ bỏ hoàn toàn `snapd` và `unattended-upgrades` giúp tiết kiệm ~80MB RAM và giải phóng gần 1GB không gian đĩa NVMe.
* **Cơ chế Swap thông minh:**
  * Tạo file `/swapfile` dung lượng **2GB** (kết hợp với swap mặc định đạt ~3.8GB).
  * Thiết lập `vm.swappiness = 10` trong `/etc/sysctl.conf`: Ép Linux ưu tiên sử dụng RAM vật lý thật, chỉ đẩy dữ liệu ít dùng vào Swap khi bộ nhớ thực đạt ngưỡng > 90%, giúp game Java không bị trễ (latency/lag).

### 2.2. Lớp Hiển Thị & Quản Lý Cửa Sổ (Display & Window Manager)
* **TigerVNC Standalone Server:**
  * Chạy trên Display `:1` (Cổng TCP `5901`).
  * Tham số bảo mật: `-localhost yes -SecurityTypes None`. Do gắn cứng lắng nghe trên `127.0.0.1`, chỉ có các phiên kết nối thông qua SSH Tunnel mới có thể truy cập được, người ngoài trên Internet hoàn toàn không thể thấy hoặc dò quét cổng VNC.
  * **Độ phân giải chuẩn 800x600:** Giảm 53% lượng pixel truyền tải so với 1280x800, giúp CPU encode hạ nhiệt và xóa bỏ hoàn toàn hiện tượng delay/lag mạng.
  * Tích hợp thành dịch vụ hệ thống `systemd` (`vncserver.service`), tự động phục hồi sau khi reboot máy chủ.
* **Openbox Window Manager + tint2 Taskbar:**
  * Openbox quản lý cửa sổ siêu nhẹ (~15MB RAM).
  * Cấu hình cờ `<maximized>yes</maximized>` giúp cửa sổ game chiếm trọn màn hình VNC.
  * Thanh tác vụ `tint2` ở cạnh đáy (30px, chỉ ngốn 3MB RAM) hiển thị rõ các tab `[acc1]`, `[acc2]`, click chuột là chuyển đổi giữa các acc ngay trong game cực nhanh.
* **Xmodmap (Chuẩn hóa phím NumPad):**
  * Hệ thống đã can thiệp ở tầng X11 qua `xmodmap`, ánh xạ:
    * `KP_7` (79) ↔ `KP_1` (87)
    * `KP_8` (80) ↔ `KP_2` (88)
    * `KP_9` (81) ↔ `KP_3` (89)
  * Người chơi chỉ cần nhìn vào bàn phím NumPad máy tính và bấm theo vị trí hiển thị skill trên màn hình game.

### 2.3. Lớp Giả Lập Game & Cô Lập Tài Khoản (Emulation & RMS Isolation)

### 2.4. Cơ Chế Auto-Tuning Cấu Hình Thấp Nhất Cho VPS Cùi 1:1 (1 vCPU / 1GB RAM)
Khi chạy trên VPS gói giá rẻ cấu hình tối thiểu (1 vCPU và 1024MB RAM):
* **Nguyên tắc Cô lập Tuyệt đối (Non-Intrusive Tenant):** VPS có thể chạy đa nhiệm (Web server, Tool API, Bot, Game Server,...). Dự án treo game J2ME hoạt động khép kín hoàn toàn trong `/opt/jar-manager/` và VNC Display `:1`. Tuyệt đối không can thiệp, tắt hoặc làm gián đoạn các dịch vụ khác trên máy chủ.
* **Tự động ép Java Heap:** Script `start-acc.sh` tự động đọc `free -m`, nếu RAM $\le$ 1024MB hoặc RAM khả dụng $\le$ 600MB sẽ tự gán `-Xms16m -Xmx32m -XX:+UseSerialGC -XX:TieredStopAtLevel=1` (tiết kiệm hơn 50% RAM so với cấu hình thông thường).
* **Tiết kiệm chu kỳ CPU:** VNC Server duy trì `800x600 -depth 16` kết hợp `FrameRate=15-20` và `CompareFB=1` giảm tải tối đa cho 1 nhân vCPU.
* **Bảo vệ bằng Swap:** Bắt buộc duy trì `/swapfile` 2GB (`swappiness=10`) để các map đông người hoặc tải sprite không kích hoạt OOM-Killer.
* **OpenJDK 8 JRE:** Bản Java chuẩn tương thích tối đa với bộ giả lập J2ME MicroEmulator.
* **MicroEmulator:**
  * Chạy với thiết bị điều chỉnh kích thước: `--resizableDevice 800 520`.
  * Khung bàn phím bấm tay cổ điển bị loại bỏ 100%, khung vẽ đồ họa (Canvas) của game được căn chỉnh vừa khít tỉ lệ màn hình ảo, giữ kích thước nhân vật, nút bấm và chữ viết to rõ, dễ nhìn, dễ click.
* **Cơ chế Cô Lập Dữ Liệu (RMS Isolation):**
  * Mỗi tài khoản khi khởi chạy được gán tham số Java riêng biệt:
    ```bash
    -Duser.home=/opt/jar-manager/profiles/<tên_acc>
    ```
  * Điều này đảm bảo mỗi tài khoản có một thư mục dữ liệu hoàn toàn độc lập, có thể đăng nhập nhiều tài khoản khác nhau trên cùng một bản game `.jar` mà không bao giờ bị đè dữ liệu lên nhau.
* **Giới hạn bộ nhớ (Memory Clamping):**
  * Mỗi tiến trình Java được giới hạn bởi cờ `-Xms32m -Xmx64m`.
  * Mỗi acc game chỉ chiếm ~60MB RAM vật lý. Với 1GB RAM, VPS có thể duy trì ổn định 3–5 acc cùng lúc mà không xảy ra hiện tượng tràn bộ nhớ.
