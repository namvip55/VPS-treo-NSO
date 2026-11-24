# HỆ THỐNG TREO GAME J2ME (.JAR) 24/7 TRÊN VPS LINUX & CLIENT WINDOWS

Hệ thống chuyên dụng tối ưu hóa tài nguyên phần cứng để treo game Java J2ME (Ninja School Online, v.v.) liên tục 24/7 trên VPS Linux cấu hình tiết kiệm (1 vCPU, 1GB - 2GB RAM, IP NAT) với mức tiêu thụ RAM cực nhẹ (< 80MB cho client), 1 cửa sổ duy nhất chiếm trọn màn hình và độ trễ thao tác gần như bằng 0 (Zero-Lag Engine 60 FPS).

---

## 📌 1. Kiến Trúc & Tính Năng Nổi Bật

* **Zero-Lag Engine 60 FPS NoJPEG (TigerVNC + Openbox):**
  - Màn hình ảo VNC chạy cố định ở độ phân giải tối ưu: `800x600 -depth 16`.
  - Tốc độ khung hình `FrameRate=60` kèm `-NoJPEG=1` loại bỏ nén ảnh mờ, hình ảnh sắc nét từng pixel, `CompareFB=1` giảm hơn 50% CPU và băng thông mạng.
  - Đảo chuẩn 100% bàn phím số NumPad qua `RemapKeys` và `xmodmap` (Phím 8 nhảy lên, phím 2 cúi xuống, phím 4/6 đi lại, phím 5 đánh, phím 7/9/1/3 dùng skill).
* **Cơ Chế Điều Tiết Động Hai Chế Độ (Dual-Mode Dynamic Scaling):**
  - **Chế độ Thao tác (Active Mode):** Khi mở VNC vào game, CPU được nâng ưu tiên (`renice -n 0`), focus cửa sổ game, chạy 60 FPS siêu mượt.
  - **Chế độ Treo Ngầm (Idle Mode):** Khi tắt cửa sổ game, tiến trình tự động hạ ưu tiên (`renice -n 15`), CPU hạ xuống < 5% để máy chủ chạy 24/7 mát mẻ.
  - **Auto Power Save:** Tự động phát hiện phiên VNC và điều tiết chế độ cho mọi tài khoản.
* **Auto-Tuning Cấu Hình Theo RAM Khả Dụng (Ultra-Low Spec Profile):**
  - Script tự động phát hiện VPS cấu hình 1:1 (1 vCPU / 1GB RAM) hoặc RAM khả dụng $\le 600\text{MB}$ để tự động ép cấu hình Java xuống `-Xms16m -Xmx32m -XX:+UseSerialGC -XX:TieredStopAtLevel=1`.
* **Nguyên Tắc Cô Lập Tuyệt Đối (Non-Intrusive Tenant):**
  - Toàn bộ dữ liệu, emulator, profile và script treo game nằm gọn gàng bên trong `/opt/jar-manager/` và VNC Display `:1`.
  - Hoàn toàn độc lập, không can thiệp hay ảnh hưởng tới các dịch vụ khác cùng chạy trên VPS (Web server Apache/Nginx, Tool API, Bot Telegram/Discord, Game Server,...).
* **Bảo Mật Tuyệt Đối Khi Giao Khách (Client Security & Wrapper):**
  - Đóng gói ứng dụng 1-click độc lập qua `tools/package_client.py` và `tools/ClientLauncher.cs`.
  - Sinh khóa SSH Ed25519 riêng cho khách, **tuyệt đối không nhúng khóa root**.
  - Áp dụng `client-cmd-wrapper.sh` và `permitopen="127.0.0.1:5901"`: Khách chỉ được mở tunnel VNC cổng 5901 và gọi các lệnh game chỉ định, bị chặn 100% quyền truy cập shell root.

---

## 📁 2. Cấu Trúc Thư Mục Dự Án

```plaintext
Vps-Treo-Nso/
├── .agents/
│   ├── AGENTS.md                  # Router điều hướng cho AI Agent
│   └── skills/                    # Bộ kỹ năng tự động hóa
│       ├── vps-jar-setup/         # Skill tự động setup VPS từ số 0
│       ├── update-game/           # Skill đổi bản game/mod mới
│       ├── update-vps/            # Skill di chuyển/cập nhật sang VPS mới
│       └── package-client/        # Skill đóng gói app 1-click cho khách
├── servers/                       # PHÂN VÙNG CÔ LẬP CHO TỪNG VPS
│   ├── vps1/                      # VPS Chính (nepsilon.jar + config.json)
│   └── vps2_sample/               # VPS Khách mẫu (HoiUc220.jar + config.json)
├── tools/                         # CÔNG CỤ TỰ ĐỘNG HÓA
│   ├── vps_manager.py             # Quản lý & update từng VPS riêng biệt
│   ├── package_client.py          # Đóng gói bộ cài 1-click cho khách (EXE/ZIP)
│   ├── ClientLauncher.cs          # Mã nguồn WinForms Client độc lập
│   └── download_vnc.py            # Tự động tải TigerVNC Viewer nếu thiếu
├── docs/                          # TÀI LIỆU KỸ THUẬT (SINGLE SOURCE OF TRUTH)
│   ├── ARCHITECTURE.md            # Thiết kế kiến trúc kỹ thuật & tối ưu RAM
│   ├── NETWORK_AND_TROUBLESHOOTING.md # Mạng NAT, DNS Round-Robin & xử lý lỗi
│   ├── OPERATIONS.md              # Sổ tay vận hành & quản lý tập lệnh
│   ├── SETUP_FORM.md              # Khung mẫu setup chuẩn
│   └── README.md                  # Tài liệu tổng quan
├── web/                           # GIAO DIỆN WEB MOBILE CHO IPHONE / IPAD
│   ├── index.html                 # Giao diện noVNC responsive chuẩn PWA
│   ├── style.css                  # Theme Glassmorphism tối ưu cảm ứng GPU
│   ├── gamepad.js                 # Bàn phím ảo cảm ứng đa năng (Mọi game Java)
│   ├── config.js                  # Cấu hình PIN và tên game động
│   └── manifest.json              # Hỗ trợ Add to Home Screen toàn màn hình
├── scripts/                       # TẬP LỆNH SHELL TRÊN VPS
│   ├── start-acc.sh               # Khởi chạy tài khoản (tự động nhận diện RAM & bypass Start)
│   ├── stop-acc.sh                # Tắt tài khoản an toàn
│   ├── slot-manager.sh            # Quản lý đa game / đa slot chia sẻ 1 link Ngrok (add, list, stop)
│   ├── setup-ios-web.sh           # Cài đặt tự động Web noVNC + Ngrok Static Tunnel
│   ├── mode-active.sh             # Nâng ưu tiên CPU (renice 0) khi xem game
│   ├── mode-idle.sh               # Hạ ưu tiên CPU (renice 15) khi treo ngầm
│   ├── auto-power-save.sh         # Tự động chuyển mode theo kết nối VNC
│   ├── client-cmd-wrapper.sh      # Wrapper giới hạn an toàn lệnh SSH cho khách
│   ├── focus-acc.sh               # Phóng to và đưa cửa sổ acc lên đầu
│   ├── status-all.sh              # Kiểm tra trạng thái toàn bộ acc
│   └── system-info.sh             # Kiểm tra RAM, Swap, CPU tải
├── .env.example                   # Mẫu cấu hình bảo mật không lộ token
├── vps_registry.json              # DANH BẠ QUẢN LÝ TẬP TRUNG MULTI-VPS
├── AGENTS.md                      # Kiến thức chuẩn & quy tắc sống còn cho Agent
└── README.md                      # Giới thiệu dự án
```

---

## 🚀 3. Hướng Dẫn Sử Dụng Nhanh

### 3.1. Nhánh 1: Bàn giao cho khách dùng Máy tính Windows (File EXE 1-Click)
```bash
# Tạo file EXE độc lập kèm đẩy public key an toàn lên VPS:
python tools/package_client.py --host 14.250.250.229 --port 12724 --title "NSO GEMINI" --acc acc1 --push-pubkey

# Kết quả file nằm tại: dist_clients/NSO_GEMINI_Client.exe
# Khách chỉ cần tải file về máy, nhấp đúp là vào thẳng game!
```

### 3.2. Nhánh 2: Bàn giao cho khách dùng iPhone / iPad (Web Mobile PWA)
```bash
# Chạy lệnh setup web & tunnel với mã PIN bảo mật riêng:
bash /opt/jar-manager/scripts/setup-ios-web.sh \
  --token <NGROK_AUTHTOKEN> \
  --domain mistily-stipular-kristopher.ngrok-free.dev \
  --pin 8888 \
  --title "TÊN GAME CỦA BẠN"

# Gửi cho khách:
# 1. Link: https://mistily-stipular-kristopher.ngrok-free.dev
# 2. Mã PIN: 8888
# Khách mở Safari -> Nhập PIN -> Bấm 'Chia sẻ' -> 'Thêm vào MH chính' là xong!
```

### 3.3. Nhánh 3: Tận Dụng RAM Dư - Chạy Thêm Game Mới Cho Khách Khác (Chia sẻ 1 link Ngrok)
```bash
# Thêm Game mới (Ví dụ: Slot 2 - Avatar 250 - Mã PIN 2222):
bash /opt/jar-manager/scripts/slot-manager.sh add 2 /opt/jar-manager/games/Avatar250.jar 2222 "Avatar 250"

# Thêm Game tiếp theo (Ví dụ: Slot 3 - Ngọc Rồng - Mã PIN 3333):
bash /opt/jar-manager/scripts/slot-manager.sh add 3 /opt/jar-manager/games/NRO.jar 3333 "Ngọc Rồng Online"

# Xem danh sách các game/slot đang chạy trên VPS:
bash /opt/jar-manager/scripts/slot-manager.sh list

# Khách 2 vào link cũ nhưng nhập PIN 2222 -> Vào đúng Avatar 250!
# Khách 3 vào link cũ nhưng nhập PIN 3333 -> Vào đúng Ngọc Rồng!
```

### 3.4. Quản lý Multi-VPS qua `vps_manager.py` (Cú pháp chuẩn hóa)
```bash
# Xem danh sách VPS trong danh bạ
python tools/vps_manager.py list

# Kiểm tra trạng thái máy chủ
python tools/vps_manager.py status vps1

# Cập nhật thông số VPS: update-vps <name-vps> <host:port>
python tools/vps_manager.py update-vps vps1 14.250.250.229:12724 --sync

# Cập nhật game: update-game <name-vps> : thay <tab-game-cũ> = <bản jar mới>
python tools/vps_manager.py update-game vps1 --tab 1 --jar servers/vps1/nepsilon.jar
python tools/vps_manager.py update-game vps1 --tab 2 --jar D:/games/Avatar250.jar

# Thu hồi & xóa slot khách: remove-client <name-vps> <tab-game-khách>
python tools/vps_manager.py remove-client vps1 slot2

# Cấu hình Web Phone điều khiển bắt buộc trên Safari/Chrome
python tools/vps_manager.py setup-web vps1 --pin 1234
```

### 3.5. Cửa Sổ Giám Sát Trung Tâm: Admin Dashboard GUI (Windows)
Chỉ cần nhấp đúp file **`admin_dashboard.bat`** (hoặc gõ `python tools/vps_manager.py gui`):
* **Giám sát thời gian thực:** Xem danh sách mọi VPS, trạng thái Online/Offline, độ trễ Ping, biểu đồ RAM, Swap, CPU load và ước lượng dung lượng còn trống (chạy thêm được bao nhiêu slot game).
* **Quản lý đa Game / Slot:** Hiển thị trực quan từng game đang chạy trên từng VPS (Slot ID, Tên game, Tài khoản, Mã PIN khách, Cổng VNC nội bộ, PID tiến trình).
* **Thao tác 1-Click tiện lợi:**
  - `[▶ Xem Live Màn Hình]`: Tự động tạo SSH tunnel và mở TigerVNC Viewer xem trực tiếp màn hình game của khách đang cày 24/7.
  - `[+ Thêm Game Slot]`: Thêm game mới (Avatar, NRO, NSO...) cho khách mới mà không cần thêm link Ngrok.
  - `[🗑 Xóa Slot / Thu Hồi]`: Tự động tắt game, thu hồi mã PIN, dọn sạch profile và cập nhật lại file cấu hình khi khách ngừng thuê.
  - `[📋 Copy Link iPhone]`: Sao chép link Safari kèm mã PIN vào clipboard để gửi cho khách Mobile.
  - `[📦 Xuất App Windows]`: 1-click đóng gói Single EXE cho khách chơi trên máy tính.

---

## ⚠️ 4. Các Quy Tắc Sống Còn Cần Nhớ
1. **Tuyệt đối KHÔNG bật UFW (`ufw enable`):** VPS NAT dùng routing qua VLAN `eth0.50`, bật UFW sẽ drop gói tin SSH gây mất kết nối vĩnh viễn.
2. **Dùng IPv4 trực tiếp:** Tránh lỗi do Round-Robin DNS của `remote.io.vn`.
3. **Không cài đè `openjdk-8-jre` nếu VPS có Server OpenJDK 17:** MicroEmulator 2.0.4 tương thích mượt mà trên OpenJDK 17.
4. **Không nhúng admin key root cho khách:** Luôn dùng `package_client.py --push-pubkey` để tạo khóa riêng với wrapper giới hạn.
