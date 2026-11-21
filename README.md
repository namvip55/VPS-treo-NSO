# HỆ THỐNG TREO GAME J2ME (.JAR) 24/7 TRÊN VPS LINUX & CLIENT WINDOWS

Hệ thống chuyên dụng tối ưu hóa tài nguyên phần cứng để treo game Java J2ME (Ninja School Online, v.v.) liên tục 24/7 trên VPS Linux cấu hình tiết kiệm (1 vCPU, 1GB - 2GB RAM, IP NAT) với mức tiêu thụ RAM cực nhẹ (< 80MB cho client), 1 cửa sổ duy nhất chiếm trọn màn hình và độ trễ thao tác gần như bằng 0 (Zero-Lag Engine).

---

## 📌 1. Kiến Trúc & Tính Năng Nổi Bật

* **Zero-Lag Engine (TigerVNC + Openbox):**
  - Màn hình ảo VNC chạy cố định ở độ phân giải tối ưu: `800x600 -depth 16`.
  - Giới hạn khung hình `FrameRate=30` (hoặc `15-20` cho VPS 1 core), `CompareFB=1` giảm hơn 50% CPU và băng thông mạng.
  - Đảo chuẩn 100% bàn phím số NumPad qua `RemapKeys` và `xmodmap` (Phím 8 nhảy lên, phím 2 cúi xuống, phím 4/6 đi lại, phím 5 đánh, phím 7/9/1/3 dùng skill).
* **Auto-Tuning Cấu Hình Theo RAM Khả Dụng (Ultra-Low Spec Profile):**
  - Script tự động phát hiện VPS cấu hình 1:1 (1 vCPU / 1GB RAM) hoặc RAM khả dụng $\le 600\text{MB}$ để tự động ép cấu hình Java xuống `-Xms16m -Xmx32m -XX:+UseSerialGC -XX:TieredStopAtLevel=1`.
* **Nguyên Tắc Cô Lập Tuyệt Đối (Non-Intrusive Tenant):**
  - Toàn bộ dữ liệu, emulator, profile và script treo game nằm gọn gàng bên trong `/opt/jar-manager/` và VNC Display `:1`.
  - Hoàn toàn độc lập, không can thiệp hay ảnh hưởng tới các dịch vụ khác cùng chạy trên VPS (Web server Apache/Nginx, Tool API, Bot Telegram/Discord, Game Server,...).
* **Ứng Dụng Điều Khiển 1-Click Trên Windows (`JarManager.exe`):**
  - Tự động thiết lập SSH Tunnel bảo mật qua cổng 5901.
  - Tự động mở TigerVNC Viewer với thông số nén tối ưu `Tight Encoding`, `QualityLevel 6`.
  - Thao tác xong chỉ cần bấm `[X]` tắt cửa sổ VNC, game trên VPS vẫn tự động cày ngầm 24/7.

---

## 📁 2. Cấu Trúc Thư Mục Dự Án

```plaintext
Vps-Treo-Nso/
├── .agents/
│   ├── AGENTS.md                  # Hướng dẫn chi tiết cho AI Agent
│   ├── docs/                      # Bản sao tài liệu kỹ thuật
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
│   └── ClientLauncher.cs          # Mã nguồn WinForms Client độc lập
├── docs/
│   ├── ARCHITECTURE.md            # Thiết kế kiến trúc kỹ thuật & tối ưu RAM
│   ├── NETWORK_AND_TROUBLESHOOTING.md # Mạng NAT, DNS Round-Robin & xử lý lỗi
│   ├── OPERATIONS.md              # Sổ tay vận hành & quản lý tập lệnh
│   ├── SETUP_FORM.md              # Khung mẫu setup chuẩn
│   └── README.md                  # Tài liệu tổng quan
├── scripts/
│   ├── start-acc.sh               # Khởi chạy tài khoản (tự động nhận diện RAM & bypass Start)
│   ├── stop-acc.sh                # Tắt tài khoản an toàn
│   ├── focus-acc.sh               # Phóng to và đưa cửa sổ acc lên đầu
│   ├── status-all.sh              # Kiểm tra trạng thái toàn bộ acc
│   └── system-info.sh             # Kiểm tra RAM, Swap, CPU tải
├── vps_registry.json              # DANH BẠ QUẢN LÝ TẬP TRUNG MULTI-VPS
├── JarManager.cs                  # Mã nguồn C# launcher ẩn console
├── JarManager.ps1                 # Giao diện điều khiển Windows WinForms
├── JarManager.exe                 # File chạy Windows 1-click
├── vncviewer.exe                  # TigerVNC Viewer Portable tối ưu
├── AGENTS.md                      # Kiến thức chuẩn cho Agent
└── README.md                      # Giới thiệu dự án
```

---

## 🚀 3. Hướng Dẫn Sử Dụng Nhanh

### 3.1. Dành cho người chơi (Windows)
1. Bấm đúp vào file `JarManager.exe` ngoài Desktop hoặc thư mục gốc.
2. Bấm nút **`▶ VAO THAO TAC GAME (1 CLICK)`**.
3. Cửa sổ game sẽ hiện lên ngay lập tức. Sau khi cài auto/treo acc, chỉ việc bấm nút `[X]` tắt cửa sổ VNC.

### 3.2. Dòng lệnh quản trị từ xa (CLI / PowerShell)
```powershell
# Bật game acc1
ssh vps-jar "/opt/jar-manager/scripts/start-acc.sh acc1"

# Tắt game acc1
ssh vps-jar "/opt/jar-manager/scripts/stop-acc.sh acc1"

# Xem danh sách acc đang chạy
ssh vps-jar "/opt/jar-manager/scripts/status-all.sh"

# Kiểm tra RAM, CPU, Swap
ssh vps-jar "/opt/jar-manager/scripts/system-info.sh"
```

---

## ⚠️ 4. Các Quy Tắc Sống Còn Cần Nhớ
1. **Tuyệt đối KHÔNG bật UFW (`ufw enable`):** VPS NAT dùng routing qua VLAN `eth0.50`, bật UFW sẽ drop gói tin SSH gây mất kết nối vĩnh viễn.
2. **Dùng IPv4 trực tiếp trong `~/.ssh/config`:** Tránh lỗi do Round-Robin DNS của `remote.io.vn`.
3. **Không cài đè `openjdk-8-jre` nếu VPS có Server OpenJDK 17:** MicroEmulator 2.0.4 tương thích mượt mà trên OpenJDK 17.
