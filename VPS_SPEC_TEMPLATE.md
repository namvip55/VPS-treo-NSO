# 📋 CẨM NANG KHAI BÁO & THÔNG SỐ VPS CHUẨN (VPS SPEC BLUEPRINT)
> **Dành cho AI Agent & Kỹ Thuật Viên:** Đọc tài liệu này để nắm vững 100% cách khai báo máy chủ VPS mới, cấu trúc dữ liệu cô lập và cơ chế cấp Key/Mã PIN cho từng tab game trên Web Phone.

---

## 📌 1. BẢNG THÔNG SỐ BẮT BUỘC CỦA MỖI MÁY CHỦ VPS

Khi tiếp nhận hoặc thêm 1 VPS mới vào hệ thống, **BẮT BUỘC** phải có đầy đủ các trường thông số sau:

| Trường thông số | Kiểu dữ liệu | Ý nghĩa | Ví dụ thực tế | Lưu ý sống còn |
| :--- | :--- | :--- | :--- | :--- |
| **`id`** | String | Mã định danh VPS (viết liền, không dấu) | `vps1`, `khach_tuan` | Dùng làm tên thư mục trong `servers/<id>/` |
| **`name`** | String | Tên gợi nhớ hiển thị trên Admin Dashboard | `VPS Khách Tuấn (NSO)` | Hiển thị trực quan cho người quản trị |
| **`host`** | String | **Địa chỉ IPv4 trực tiếp** của máy chủ | `14.250.250.229` | **TUYỆT ĐỐI KHÔNG DÙNG** domain xoay tua `remote.io.vn` |
| **`port`** | Integer | Cổng SSH NAT do nhà mạng cấp | `12724`, `10892` | Ánh xạ qua NAT router của nhà cung cấp |
| **`user`** | String | Tài khoản đăng nhập máy chủ | `root` | Mặc định là `root` |
| **`key`** | String | Đường dẫn SSH Private Key quản trị | `~/.ssh/id_ed25519_jar` | Sử dụng khóa Ed25519 quản trị |
| **`game`** | String | Tên file game `.jar` mặc định của VPS | `nepsilon.jar`, `game.jar` | File JAR đặt trong `servers/<id>/` |
| **`game_title`** | String | Tiêu đề tựa game | `NSO GEMINI`, `Avatar 250` | Hiển thị trên thanh tiêu đề Web Phone |
| **`web_url`** | String | Đường link Web Phone điều khiển của VPS | `https://<domain>.ngrok-free.dev` | Link Ngrok Static hoặc Cloudflare Tunnel riêng |
| **`default_pin`** | String | Mã PIN truy cập mặc định cho Tab 1 | `1234`, `8888` | Mã số 4 ký tự cấp cho chủ máy hoặc khách Tab 1 |
| **`accounts`** | Array | Danh sách tên profile acc của VPS | `["acc1"]` | Thư mục RMS tương ứng trong `/opt/jar-manager/profiles/` |
| **`notes`** | String | Ghi chú cấu hình dịch vụ | `Treo game NSO + Web Phone` | Ghi chú thêm nếu VPS có chạy đa nhiệm |

---

## 📁 2. CẤU TRÚC PHÂN VÙNG ĐỘC LẬP (`servers/<name-vps>/`)

Mỗi VPS được quản lý trong một không gian riêng biệt, không đè dữ liệu lên nhau:

```text
Vps-Treo-Nso/
├── vps_registry.json                  # DANH BẠ TẬP TRUNG TẤT CẢ CÁC VPS
└── servers/
    └── <name-vps>/                    # THƯ MỤC RIÊNG CỦA VPS NÀY
        ├── config.json                # File cấu hình thông số riêng
        └── <ten_file_game>.jar        # Bản game JAR riêng của VPS này
```

### Mẫu file `servers/<name-vps>/config.json`:
```json
{
  "id": "vps_khach1",
  "name": "VPS Khách Tuấn (NSO)",
  "dir": "servers/vps_khach1",
  "host": "14.250.250.229",
  "port": 12724,
  "user": "root",
  "key": "~/.ssh/id_ed25519_jar",
  "game": "game.jar",
  "game_title": "NSO GEMINI",
  "web_url": "https://domain-cua-khach.ngrok-free.dev",
  "default_pin": "1234",
  "accounts": [
    "acc1"
  ],
  "notes": "VPS cấu hình 1 vCPU / 1GB RAM treo game 24/7"
}
```

### Mẫu danh bạ `vps_registry.json`:
```json
{
  "active_vps": "vps_khach1",
  "servers": {
    "vps_khach1": {
      "id": "vps_khach1",
      "name": "VPS Khách Tuấn (NSO)",
      "dir": "servers/vps_khach1",
      "host": "14.250.250.229",
      "port": 12724,
      "user": "root",
      "key": "~/.ssh/id_ed25519_jar",
      "game": "game.jar",
      "game_title": "NSO GEMINI",
      "web_url": "https://domain-cua-khach.ngrok-free.dev",
      "default_pin": "1234",
      "accounts": [
        "acc1"
      ],
      "notes": "VPS mẫu chuẩn"
    }
  }
}
```

---

## 🔑 3. CƠ CHẾ KEY (MÃ PIN) ĐIỀU KHIỂN TỪNG TAB GAME TRÊN WEB PHONE

Mỗi VPS chỉ cần **1 đường link Web tĩnh duy nhất**, nhưng có thể treo cùng lúc nhiều Tab Game (Slot) độc lập. **Mỗi Tab Game sở hữu một Mã PIN (Key) riêng biệt**:

```text
       [ĐƯỜNG LINK WEB PHONE CỦA VPS: https://<domain>.ngrok-free.dev]
                                     |
    +--------------------------------+--------------------------------+
    |                                |                                |
[TAB 1: GAME GỐC]            [TAB 2: KHÁCH THUÊ A]            [TAB 3: KHÁCH THUÊ B]
- Màn hình VNC: Port 5901     - Màn hình VNC: Port 5902        - Màn hình VNC: Port 5903
- Profile RMS: profiles/acc1  - Profile RMS: profiles/slot2    - Profile RMS: profiles/slot3
- MÃ PIN (KEY): 1234          - MÃ PIN (KEY): 2222             - MÃ PIN (KEY): 3333
- Link vào thẳng:             - Link vào thẳng:                - Link vào thẳng:
  https://.../?pin=1234         https://.../?pin=2222            https://.../?pin=3333
```

### Quy tắc bảo mật:
1. Khách nhập mã PIN nào thì hệ thống `websockify` bẻ luồng WebSocket vào đúng cổng VNC nội bộ của tab đó.
2. Khách Tab 2 **tuyệt đối không thể nhìn thấy màn hình hay can thiệp vào Tab 1 hoặc Tab 3**.
3. Khi khách ngừng thuê: Chỉ cần xóa slot của khách đó (`remove-client <name-vps> <tab>`), mã PIN bị hủy ngay lập tức, dữ liệu tài khoản bị xóa sạch bảo mật.

---

## ⚡ 4. BỐN LỆNH ĐIỀU HÀNH BẮT BUỘC DÀNH CHO AGENT

Mọi thao tác quản lý phải gắn liền với **1 VPS cụ thể** (`<name-vps>`):

### 4.1. Cập nhật thông số VPS:
```powershell
python tools/vps_manager.py update-vps <name-vps> <host:port> [options]
# Ví dụ:
python tools/vps_manager.py update-vps vps_khach1 14.250.250.229:12724 --sync
```

### 4.2. Cập nhật game cho tab chỉ định:
```powershell
python tools/vps_manager.py update-game <name-vps> --tab <tab_cu> --jar <file_jar_moi>
# Ví dụ: Thay tab 2 của khách bằng game Avatar mới:
python tools/vps_manager.py update-game vps_khach1 --tab 2 --jar "D:\games\Avatar250.jar"
```

### 4.3. Cấu hình điều khiển qua Web Phone (Bắt buộc) — Không tự ý đóng gói:
```powershell
python tools/vps_manager.py setup-web <name-vps> --pin 1234
```
* **LƯU Ý:** Giao diện mặc định là Web Phone. **Chỉ khi người dùng yêu cầu rõ ràng** (*"đóng gói app", "xuất file exe", "giao client PC"*, gọi `package-client`) thì mới đóng gói app Windows.

### 4.4. Thu hồi tab game khi khách ngừng thuê:
```powershell
python tools/vps_manager.py remove-client <name-vps> <tab-game-khach>
# Ví dụ: Thu hồi Tab 2:
python tools/vps_manager.py remove-client vps_khach1 slot2
```

---

## ⚠️ 5. CÁC QUY TẮC CẤM KỴ SỐNG CÒN
1. **TUYỆT ĐỐI KHÔNG BẬT UFW:** VPS IP NAT định tuyến qua `eth0.50`, bật UFW sẽ làm mất kết nối SSH vĩnh viễn.
2. **LUÔN DÙNG IPV4 TRỰC TIẾP:** Cấm dùng domain Round-Robin `remote.io.vn` làm Host trong cấu hình.
3. **CÔ LẬP KHÔNG ĐỤNG CHẠM DỰ ÁN KHÁC:** Chỉ thao tác bên trong `/opt/jar-manager/`, không can thiệp Web, API, Bot hoặc Game Server khác đang chạy trên VPS.
