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
* **Host Remote:** `remote.io.vn` / `14.250.250.229`
* **Port NAT (SSH & Game):** `12724` (ánh xạ qua bộ ghép kênh sslh trên cổng 3389 vào SSH 22 & Game 12724)
* **User quản trị:** `root` | **Mật khẩu:** `SPF2#!xGBnKk8BwL`
* **SSH Key cá nhân:** `C:\Users\Admin\.ssh\id_ed25519_jar` (Ed25519) | Alias: `vps-jar` trong `~/.ssh/config`
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
   * Khi kiểm tra tài nguyên VPS thấy CPU = 1 core, tổng RAM $\le$ 1024 MB, hoặc RAM khả dụng (Available RAM) thấp, hệ thống **BẮT BUỘC TỰ ĐỘNG ÁP DỤNG PROFILE SETUP THẤP NHẤT (ULTRA-LOW PROFILE)**:
     - **Java Heap MicroEmulator:** Ép RAM ở mức cực thấp `-Xms16m -Xmx32m` (tối đa 48m), kích hoạt `-XX:+UseSerialGC -XX:TieredStopAtLevel=1` để triệt tiêu ngốn RAM của bộ biên dịch JIT.
     - **Màn hình ảo VNC:** Cố định `800x600 -depth 16`, hạ `FrameRate=15` (hoặc 20 FPS), `CompareFB=1` giúp giảm 65% tải CPU cho 1 vCPU.
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

---

## 🖥️ 3. KIẾN TRÚC HIỂN THỊ CHỐNG LAG (ZERO-LAG ENGINE)

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
    FrameRate=30
    CompareFB=1
    RemapKeys=0xffb8<>0xffb2,0xffb7<>0xffb1,0xffb9<>0xffb3,0xff97<>0xff99,0xff95<>0xff9c,0xff9a<>0xff9b
    ```
  * `FrameRate=30` + `CompareFB=1`: Giảm 50% CPU và băng thông so với 60 FPS mặc định.

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
  * Giới hạn RAM: `-Xms32m -Xmx64m`.

* **Client Windows ([JarManager.exe](file:///C:/JarManager/JarManager.exe)):**
  * Tự động tạo SSH Tunnel qua cổng 5901.
  * Mở VNC Viewer với cờ tối ưu:
    `localhost:5901 -PreferredEncoding=Tight -QualityLevel=6 -CompressLevel=1 -AutoSelect=0 -CustomCompressLevel=1`

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
└── scripts/
    ├── start-acc.sh                # Bật game: start-acc.sh [tên_acc]
    ├── stop-acc.sh                 # Tắt game: stop-acc.sh [tên_acc]
    ├── status-all.sh               # Xem trạng thái: status-all.sh
    └── system-info.sh              # Xem RAM, Swap, CPU: system-info.sh
```

---

## 🚀 5. DANH MỤC SKILLS HỖ TRỢ TRONG PROJECT

1. **[vps-jar-setup](.agents/skills/vps-jar-setup/SKILL.md):** Cẩm nang setup toàn diện và script tự động deploy VPS sạch từ đầu trong 60 giây.
2. **[update-game](.agents/skills/update-game/SKILL.md):** Hướng dẫn đổi sang game .jar khác hoặc nạp bản mod mới trong 10 giây qua symlink.
3. **[update-vps](.agents/skills/update-vps/SKILL.md):** Quy trình tự động di chuyển toàn bộ hệ thống sang một máy chủ VPS mới cùng loại trong 1 phút.
