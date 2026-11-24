# ANTIGRAVITY AGENT INSTRUCTIONS & KNOWLEDGE BASE
## DỰ ÁN: HỆ THỐNG TREO GAME J2ME (.JAR) 24/7 TRÊN VPS UBUNTU & CLIENT WINDOWS

Tài liệu này được thiết kế để bất kỳ AI Agent nào khi bước vào project (hoặc project mới được sao chép từ thư mục này) sẽ ngay lập tức nắm vững 100% kiến trúc, quy tắc cấm kỵ, cấu hình phần cứng, mạng NAT, và các quy trình vận hành.

---

## 📌 1. BẢNG THÔNG TIN HỆ THỐNG VPS HIỆN TẠI

* **Nhà cung cấp VPS:** `vps1k.net`
* **Hệ điều hành:** Ubuntu 22.04 LTS x86_64 (Minimal)
* **Cấu hình phần cứng:** 1 vCPU, 2048 MB RAM, 15 GB NVMe (SCSI), 2 GB Swap (`/swapfile`), Tổng Swap ~3.8 GB
* **Băng thông:** 40 Mbps (5 MB/s)
* **Hostname:** `node-h3og603c`
* **Loại mạng:** IP NAT (Việt Nam)
* **Host Remote:** `14.250.250.229` (IPv4 trực tiếp, TUYỆT ĐỐI không dùng `remote.io.vn` do Round-Robin DNS)
* **Port NAT (SSH & Game):** `12724` (ánh xạ qua bộ ghép kênh sslh trên cổng 3389 vào SSH 22 & Game 12724)
* **User quản trị:** `root` | **Mật khẩu:** `<ROOT_PASSWORD>` *(Xác thực qua SSH Key cá nhân)*
* **SSH Key quản trị:** `C:\Users\Admin\.ssh\id_ed25519_jar` (Ed25519) | Alias: `vps-jar` trong `~/.ssh/config`
* **Tên miền máy chủ:** `nguyenthanhnam376.id.vn`
* **Game JAR:** `nepsilon.jar` (Symlink: `game.jar -> nepsilon.jar`)

---

## ⚠️ 2. CÁC QUY TẮC SỐNG CÒN (CRITICAL RULES)

1. **TUYỆT ĐỐI KHÔNG BẬT UFW (`ufw enable`):**
   * *Nguyên nhân:* VPS nằm sau NAT, định tuyến qua bảng `ssh_rt` trên interface `eth0.50`. Khi bật UFW, bộ lọc `rp_filter` của Linux sẽ chặn toàn bộ các gói tin phản hồi SSH làm mất kết nối vĩnh viễn (buộc phải Reinstall OS).
   * *Bảo mật thực tế:* Router phần cứng của nhà mạng đã chặn 100% các cổng từ internet, chỉ mở duy nhất cổng SSH NAT đã ánh xạ. VNC chỉ bind `localhost`. Không cần và không được cài UFW.

2. **CẢNH BÁO ROUND-ROBIN DNS CỦA `remote.io.vn`:**
   * Tên miền `remote.io.vn` của nhà cung cấp sử dụng cơ chế DNS Round-Robin trỏ về hơn 8 cụm máy chủ khác nhau (`14.250.250.229`, `14.236.172.236`, `14.242.71.192`,...).
   * Trong file `~/.ssh/config` và các script kết nối, **BẮT BUỘC dùng địa chỉ IPv4 trực tiếp** (ví dụ: `14.250.250.229`), TUYỆT ĐỐI KHÔNG dùng `remote.io.vn` để tránh bị lỗi `Connection timed out` hoặc nhảy sang cụm node khác.

3. **TƯƠNG THÍCH JAVA RUNTIME TRÊN HỆ THỐNG DUAL-STACK (SERVER + CLIENT):**
   * Server NSO chạy trên **OpenJDK 17**. MicroEmulator 2.0.4 chạy mượt mà trên **OpenJDK 17**.
   * **TUYỆT ĐỐI KHÔNG cài thêm `openjdk-8-jre`** nếu VPS đang có Server NSO. Lệnh cài OpenJDK 8 sẽ tự động trỏ lệnh `/usr/bin/java` về Java 8, khiến `nso-server.service` crash ngay lập tức do lỗi `UnsupportedClassVersionError` (Java 17 vs Java 8).

4. **LỆNH `changeip` KHI CÀI MỚI VPS:**
   * Sau khi cài lại Ubuntu từ web, card mạng chưa có internet. Luôn phải chạy:
     `echo -e "1\ny" | /usr/local/bin/changeip`

5. **CƠ CHẾ CHẠY NỀN 24/7 (`setsid`):**
   * Mọi lệnh khởi chạy Java / MicroEmulator phải được bọc trong `setsid`:
     `setsid java ... > /dev/null 2>&1 &`
   * Điều này đảm bảo tiến trình game hoàn toàn tách biệt khỏi phiên SSH/VNC; tắt app hay tắt máy tính thì game vẫn cày 24/7.

6. **CÔ LẬP DỮ LIỆU TÀI KHOẢN (RMS ISOLATION):**
   * Mỗi tài khoản phải truyền cờ: `-Duser.home=/opt/jar-manager/profiles/<tên_acc>`
   * Tránh việc các tài khoản đè mật khẩu hoặc cài đặt lên nhau.

7. **TỰ ĐỘNG CHỌN CẤU HÌNH TỐI THIỂU CHO VPS YẾU (CẤU HÌNH 1:1 - 1 vCPU / 1GB RAM HOẶC RAM KHẢ DỤNG THẤP):**
   * Khi kiểm tra tài nguyên VPS thấy CPU = 1 core, tổng RAM $\le$ 1100 MB, hoặc RAM khả dụng (Available RAM) $\le$ 600 MB, hệ thống **BẮT BUỘC TỰ ĐỘNG ÁP DỤNG PROFILE SETUP THẤP NHẤT (ULTRA-LOW PROFILE)**:
     - **Java Heap MicroEmulator:** Ép RAM ở mức cực thấp `-Xms16m -Xmx32m` (tối đa 48m), kích hoạt `-XX:+UseSerialGC -XX:TieredStopAtLevel=1` để triệt tiêu ngốn RAM của bộ biên dịch JIT.
     - **Màn hình ảo VNC:** Cố định `800x600 -depth 16`, `CompareFB=1` giúp giảm 65% tải CPU cho 1 vCPU.
     - **Kích hoạt 2GB Swap khẩn cấp:** Đảm bảo có Swapfile 2GB (`vm.swappiness=10`) để bảo hiểm bộ nhớ, chống OOM-Killer tắt game khi map đông người.
     - **Giới hạn số Acc:** Khuyến nghị chỉ treo 1 tài khoản (tối đa 2 acc nhẹ).

8. **NGUYÊN TẮC CÔ LẬP TUYỆT ĐỐI & KHÔNG XÂM PHẠM CÁC DỰ ÁN KHÁC (STRICT ISOLATION & NON-INTRUSIVE):**
   * VPS có thể chạy đa nhiệm rất nhiều dịch vụ: **Web server (Apache/Nginx), Tool API, Bot (Telegram/Discord/Zalo), Game Server, CSDL, v.v.**
   * Nhiệm vụ duy nhất của dự án này là **LÀM TRÒN TRÁCH NHIỆM TREO GAME J2ME TRÊN VPS**, tuyệt đối không biến mình thành chủ nhân can thiệp toàn bộ máy chủ.
   * **CẤM TUYỆT ĐỐI:**
     - Không tự ý tắt, gỡ bỏ, sửa cấu hình hoặc làm gián đoạn bất kỳ tiến trình nào của các dự án khác (Apache, Nginx, MariaDB, Docker, Bot, API Tools, Game Server,...).
     - Không xóa hoặc di chuyển dữ liệu ngoài phạm vi thư mục `/opt/jar-manager/`.
     - Toàn bộ hoạt động treo game chỉ được phép diễn ra khép kín trong `/opt/jar-manager/` và màn hình ảo TigerVNC Display `:1`.
     - Phải luôn linh hoạt thích ứng theo **RAM khả dụng (Available RAM)** thực tế của VPS để cùng chung sống hòa bình với các dịch vụ khác mà không gây nghẽn máy chủ.

9. **NGUYÊN TẮC BẢO MẬT KHÓA SSH GIAO KHÁCH (CLIENT KEY ISOLATION & WRAPPER):**
   * Tuyệt đối **KHÔNG cấp khóa SSH của admin (`id_ed25519_jar`) vào file EXE hoặc gói ZIP giao cho khách**.
   * Mỗi khách hàng/máy chủ phải sinh một cặp khóa Ed25519 riêng.
   * Public key của khách đưa lên VPS **BẮT BUỘC gán wrapper bảo vệ** trong `/root/.ssh/authorized_keys`:
     `command="/opt/jar-manager/scripts/client-cmd-wrapper.sh",permitopen="127.0.0.1:5901",no-agent-forwarding,no-X11-forwarding,no-user-rc ssh-ed25519 ...`
   * Nhờ đó, khách chỉ được mở SSH tunnel cổng VNC 5901 và gọi các lệnh quản lý game được chỉ định; cấm 100% quyền truy cập shell root.

---

## 🖥️ 3. KIẾN TRÚC HIỂN THỊ CHỐNG LAG (ZERO-LAG 60 FPS ENGINE)

* **TigerVNC Server:**
  * Dịch vụ systemd: `/etc/systemd/system/vncserver.service`
  * Display: `:1` (Cổng `127.0.0.1:5901`)
  * Độ phân giải chuẩn: **`800x600 -depth 16`**
  * File cấu hình `/root/.vnc/config`:
    ```ini
    geometry=800x600
    depth=16
    localhost
    SecurityTypes=None
    FrameRate=60
    CompareFB=1
    RemapKeys=0xffb8<>0xffb2,0xffb7<>0xffb1,0xffb9<>0xffb3,0xff97<>0xff99,0xff95<>0xff9c,0xff9a<>0xff9b
    ```
  * `FrameRate=60` + `CompareFB=1`: Tốc độ 60 khung hình/giây cực mượt kết hợp thuật toán so sánh framebuffer giảm 50% CPU và băng thông.

* **Cơ chế Điều Tiết Tài Nguyên Động Hai Chế Độ (Dual-Mode Dynamic Scaling):**
  * **Chế độ Thao tác (Active Mode - `mode-active.sh`):** Khi người dùng mở VNC vào game, CPU được nâng ưu tiên (`renice -n 0`), focus cửa sổ game, hiển thị 60 FPS NoJPEG sắc nét từng pixel.
  * **Chế độ Treo Ngầm (Idle Mode - `mode-idle.sh`):** Khi người dùng đóng cửa sổ game, tiến trình Java tự động hạ ưu tiên (`renice -n 15`), đưa mức tiêu thụ CPU về < 5% để máy chủ chạy 24/7 mát mẻ.
  * **Bộ tự động điều tiết (`auto-power-save.sh`):** Kiểm tra kết nối VNC port 5901 và áp dụng chế độ phù hợp cho tất cả tài khoản.

* **Bàn Phím Số NumPad Chuẩn 100% (Đã khắc phục lỗi phím ngược):**
  * **Phím 8:** Nhảy Lên (Gửi mã 2 vào game)
  * **Phím 2:** Cúi / Đi Xuống (Gửi mã 8 vào game)
  * **Phím 4 / 6:** Trái / Phải
  * **Phím 5:** Đánh thường / Chọn / Nói chuyện
  * **Phím 7 / 9:** Skill 1 / Skill 3
  * **Phím 1 / 3:** Skill 7 / Skill 9

* **Giả Lập Java MicroEmulator 2.0.4:**
  * Khung vẽ Canvas: `--resizableDevice 800 550` (1 cửa sổ duy nhất chiếm trọn màn hình, nhân vật và chữ to rõ, không bị teo nhỏ).
  * Bộ dọn rác nhẹ: `-XX:+UseSerialGC` (CPU giảm xuống còn < 5%).
  * Tùy biến RAM: Tự động chọn 32m-64m (bình thường) hoặc 16m-32m (Ultra-low).

* **Client Điều Khiển Windows ([ClientLauncher.cs](tools/ClientLauncher.cs)):**
  * Biên dịch bằng `csc.exe` (.NET 4.0) có sẵn trên Windows.
  * Tự động tạo SSH Tunnel qua cổng nội bộ linh hoạt (5901..5950).
  * Tự động set ACL cá nhân chống lỗi key permissions của OpenSSH.
  * Mở VNC Viewer với cờ tối ưu:
    `localhost:<port> -PreferredEncoding=Tight -NoJPEG=1 -CompressLevel=1 -AutoSelect=0 -CustomCompressLevel=1`

* **Client Web Mobile Cho iPhone / iPad / Android ([web/](web/)):**
  * Chạy trực tiếp trên trình duyệt Safari / Chrome (Hỗ trợ PWA Add to Home Screen toàn màn hình).
  * Cầu nối `websockify` (Port 6080) kết nối vào TigerVNC `:1` (Port 5901).
  * **Bàn phím ảo cảm ứng đa năng (Universal Multi-Game Keypad):** Hỗ trợ chuẩn phím số Nokia T9 (`8, 2, 4, 6, 5 OK, 7, 9, 1, 3, * Menu, # Thoát, 0 Nhặt`), tương thích 100% mọi game Java (Avatar, Ngọc Rồng, Hiệp Sĩ, Mobi Army, Ninja School,...), có nút `⚡ Phím Số` chuyển đổi linh hoạt.
  * **Khóa bảo mật mã PIN riêng biệt (PIN Lock Gate):** Mỗi VPS có mã PIN riêng (Ví dụ: `--pin 8888`), chống 100% người ngoài xem lén, hỗ trợ ghi nhớ trên iPhone để vào thẳng 1-chạm.
  * **Tăng tốc phần cứng GPU Zero-Lag:** Ép vẽ GPU (`translateZ(0)`), triệt tiêu độ trễ 300ms của iOS, giữ nét pixel art chống nhòe font chữ.
  * Cố định link vĩnh viễn 100% bằng **Ngrok Permanent Static Tunnel** (`https://<domain>.ngrok-free.dev`), không bị mất kết nối hay đổi link khi reboot.

* **Cửa Sổ Quản Trị Trung Tâm Admin Dashboard GUI ([tools/AdminDashboard.cs](tools/AdminDashboard.cs)):**
  * Khởi chạy nhanh bằng file [admin_dashboard.bat](admin_dashboard.bat) hoặc `python tools/vps_manager.py gui`.
  * Giám sát thời gian thực mọi VPS: Online/Offline, Ping ms, thanh trạng thái RAM trực quan, Swap, CPU load và ước lượng sức chứa thêm slot game.
  * Quản lý trực quan từng Slot Game trên từng VPS: Slot ID, Tên game, Tài khoản, Mã PIN, Cổng VNC, PID.
  * Tích hợp 1-click: Xem trực tiếp màn hình game khách (VNC qua SSH tunnel an toàn), thêm slot game mới, xóa dọn dẹp slot khi khách thôi thuê, copy link Safari cho iPhone, và xuất file EXE cho Windows.


---

## 📁 4. CẤU TRÚC THƯ MỤC TRÊN VPS

```text
/opt/jar-manager/
├── emulator/
│   └── microemulator.jar          # MicroEmulator 2.0.4
├── games/
│   ├── nepsilon.jar                # File game Java NSO GEMINI
│   └── game.jar -> nepsilon.jar    # Symlink game mặc định
├── profiles/
│   └── acc1/                       # Thư mục dữ liệu RMS tài khoản
│       ├── acc1.pid                # Mã tiến trình PID
│       └── .microemulator/         # Dữ liệu game lưu trữ
├── web/                            # Giao diện Web Mobile cho iPhone
│   ├── core/                       # Core noVNC RFB modules
│   ├── index.html                  # Giao diện web mobile responsive
│   ├── style.css                   # Glassmorphism theme, tối ưu cảm ứng
│   ├── gamepad.js                  # Xử lý cảm ứng D-Pad 8 2 4 6 5 7 9
│   └── manifest.json               # Cấu hình PWA toàn màn hình iOS
└── scripts/
    ├── start-acc.sh                # Bật game: start-acc.sh [tên_acc] [file_jar] [màn_hình_display]
    ├── stop-acc.sh                 # Tắt game: stop-acc.sh [tên_acc]
    ├── slot-manager.sh             # Quản lý đa game / đa slot chia sẻ 1 link Ngrok (add, list, stop)
    ├── setup-ios-web.sh            # Cài noVNC + Ngrok tunnel cho iPhone
    ├── mode-active.sh              # Kích hoạt 60 FPS, renice 0 khi đang xem
    ├── mode-idle.sh                # Hạ renice 15 khi treo ngầm 24/7
    ├── auto-power-save.sh          # Tự động điều tiết active/idle theo kết nối
    ├── client-cmd-wrapper.sh       # Giới hạn an toàn lệnh SSH cho khách
    ├── status-all.sh               # Xem trạng thái: status-all.sh
    └── system-info.sh              # Xem RAM, Swap, CPU: system-info.sh
```

---

## 🚀 5. DANH MỤC SKILLS HỖ TRỢ TRONG PROJECT

1. **[vps-jar-setup](.agents/skills/vps-jar-setup/SKILL.md):** Cẩm nang setup toàn diện và script tự động deploy VPS sạch từ đầu trong 60 giây.
2. **[update-game](.agents/skills/update-game/SKILL.md):** Hướng dẫn đổi sang game .jar khác hoặc nạp bản mod mới trong 10 giây qua symlink.
3. **[update-vps](.agents/skills/update-vps/SKILL.md):** Quy trình tự động di chuyển toàn bộ hệ thống sang một máy chủ VPS mới cùng loại trong 1 phút.
4. **[package-client](.agents/skills/package-client/SKILL.md):** Quy trình và tập lệnh tự động đóng gói ứng dụng điều khiển 1-click giao cho khách (Single EXE hoặc Portable ZIP).
5. **[remove-client](.agents/skills/remove-client/SKILL.md):** Quy trình thu hồi quyền truy cập, tắt game và xóa sạch dữ liệu bảo mật khi khách ngừng thuê slot hoặc ngừng thuê VPS.

---

## 🌐 6. QUY CHUẨN QUẢN LÝ MULTI-VPS & ĐIỀU KHIỂN GAME (MULTI-NODE ZERO-CONFUSION PROTOCOL)

Khi dự án quản lý nhiều VPS cùng lúc (VPS cá nhân, VPS khách hàng), mọi AI Agent và lập trình viên **BẮT BUỘC TUÂN THỦ** 5 nguyên tắc cốt lõi:

### 6.1. Tất cả Skill phải thao tác trên 1 VPS cụ thể trong danh sách:
* Mọi hành động (`setup`, `update-vps`, `update-game`, `remove-client`, `status`) **BẮT BUỘC gắn liền với 1 VPS cụ thể** (`<name-vps>` / `<vps_id>`) trong danh bạ [vps_registry.json](file:///d:/project/Vps-Treo-Nso/vps_registry.json).
* Cấm tuyệt đối thao tác chung chung hoặc đoán mò khi chưa rõ VPS đích.

### 6.2. Cú pháp `update-vps <name-vps> <thông số>`:
* Cập nhật thông số kết nối (Host, Port NAT, Tên, SSH Key, Web URL...) của đúng VPS đó trong `vps_registry.json` và `servers/<name-vps>/config.json`.
* Ví dụ:
  ```powershell
  python tools/vps_manager.py update-vps vps1 14.250.250.229:12724 --sync
  ```

### 6.3. Cú pháp `update-game <name-vps> : thay <tab-game-cũ> = <bản jar mới>`:
* Trên một VPS có thể chạy nhiều tab/slot game (Slot 1 là game gốc, Slot 2, 3... là game của khách).
* Khi cập nhật game, Agent phải xác định đúng tab/slot game cần thay thế và file JAR mới:
  ```powershell
  # Thay thế game gốc (Tab 1 / Slot 1):
  python tools/vps_manager.py update-game vps1 --tab 1 --jar "path/to/game_moi.jar"

  # Thay thế game của tab khách (ví dụ Tab 2):
  python tools/vps_manager.py update-game vps1 --tab 2 --jar "path/to/avatar250.jar"
  ```
* **Nguyên tắc cô lập:** Chỉ thay thế file JAR và restart đúng tab/slot chỉ định. Toàn bộ các tab game khác trên VPS được giữ nguyên 100%.

### 6.4. Mặc định điều khiển qua Web Phone (BẮT BUỘC) - Không tự ý đóng gói:
* **BẮT BUỘC:** Mọi VPS và mọi slot game đều phải được cấu hình giao diện **Web Mobile Phone** (noVNC + websockify Token Routing + Web Virtual Gamepad + mã PIN riêng) để khách/chủ chơi trực tiếp trên Safari / Chrome.
* **KHÔNG TỰ Ý ĐÓNG GÓI:** Nếu người dùng **KHÔNG yêu cầu skill `package-client`**, Agent **TUYỆT ĐỐI KHÔNG TỰ Ý ĐÓNG GÓI** bộ cài `NSO_Client.exe` hay file ZIP. Mặc định chỉ cung cấp đường link Web Phone kèm mã PIN. Chỉ khi User có yêu cầu rõ ràng (*"đóng gói app cho khách", "xuất file exe", "giao client PC"*, hoặc gọi `package-client`) thì mới xuất file EXE/ZIP.

### 6.5. Cú pháp `remove-client <name-vps> <tab-game-khách>`:
* Khi khách ngừng thuê, phải trỏ đúng tab game / slot game mà khách đó thuê trên VPS đó:
  ```powershell
  python tools/vps_manager.py remove-client vps1 slot2
  # hoặc:
  python tools/vps_manager.py remove-client vps1 2
  ```
* Thu hồi chính xác mã PIN của khách trong `tokens.cfg`, dừng game và VNC của tab đó, dọn sạch dữ liệu RMS trong `profiles/slotN`, hoàn trả RAM cho VPS mà không ảnh hưởng bất kỳ tab game nào khác.

### 6.6. Cấu trúc phân vùng độc lập `servers/<vps_id>/`:
Mỗi VPS sở hữu một không gian độc lập tuyệt đối:
```text
servers/
├── vps1/                          # VPS Chính
│   ├── config.json                # Thông số: Host, Port, Game, Key, Acc, WebUrl
│   └── nepsilon.jar               # File game riêng của vps1
└── vps2_sample/                   # VPS Khách mẫu
    ├── config.json                # Thông số riêng của vps2
    └── HoiUc220.jar               # File game riêng của vps2
```
