# SỔ TAY VẬN HÀNH & QUẢN LÝ TẬP LỆNH (OPERATIONS MANUAL)

Tài liệu hướng dẫn chi tiết cách vận hành, bảo trì, thêm bản game mod mới và xử lý sự cố trong quá trình sử dụng hệ thống treo game trên VPS.

---

## 📁 1. Cấu Trúc Thư Mục Trên VPS

Toàn bộ hệ thống quản lý game được tập trung tại thư mục chuẩn `/opt/jar-manager/`:

```plaintext
/opt/jar-manager/
├── emulator/
│   └── microemulator.jar          # File chạy giả lập Java J2ME
├── games/
│   ├── HoiUc220.jar               # File game gốc/mod hiện tại
│   └── game.jar                   # Symlink trỏ tới bản game mặc định
├── profiles/
│   ├── acc1/                      # Chứa dữ liệu RMS của acc1 & file acc1.pid
│   ├── acc2/                      # Chứa dữ liệu RMS của acc2 & file acc2.pid
│   └── ...
└── scripts/
    ├── start-acc.sh               # Khởi chạy một tài khoản game
    ├── stop-acc.sh                # Dừng một tài khoản game
    ├── focus-acc.sh               # Đưa cửa sổ acc lên đầu & phóng to màn hình
    ├── status-all.sh              # Kiểm tra trạng thái toàn bộ acc
    └── system-info.sh             # Kiểm tra RAM, CPU, Swap
```

---

## 🛠️ 2. Chi Tiết Các Tập Lệnh Vận Hành

Tất cả các lệnh dưới đây có thể chạy trực tiếp trên VPS hoặc chạy từ máy tính Windows qua tiền tố:
`ssh vps-jar "<lệnh>"`

### 2.1. Khởi chạy tài khoản (`start-acc.sh`)
* **Cú pháp:** `/opt/jar-manager/scripts/start-acc.sh <tên_acc> [đường_dẫn_file_jar]`
* **Ví dụ:**
  ```bash
  # Chạy acc3 với bản game mặc định (HoiUc220.jar)
  ssh vps-jar "/opt/jar-manager/scripts/start-acc.sh acc3"

  # Chạy acc4 với một bản mod riêng biệt
  ssh vps-jar "/opt/jar-manager/scripts/start-acc.sh acc4 /opt/jar-manager/games/mod_pro.jar"
  ```
* **Cơ chế hoạt động:**
  1. Kiểm tra file PID cũ. Nếu tiến trình cũ vẫn đang sống, trả về `ACC_ALREADY_RUNNING` và không khởi chạy đè.
  2. Tạo thư mục profile riêng `/opt/jar-manager/profiles/<tên_acc>/`.
  3. Khởi chạy qua `setsid` để tiến trình hoàn toàn tách biệt khỏi phiên làm việc SSH (tắt SSH hay tắt máy tính game vẫn chạy 24/7).
  4. Sau 2 giây, tự động tìm ID cửa sổ của tiến trình và phóng to tối đa màn hình.

### 2.2. Chuyển đổi và Phóng to cửa sổ (`focus-acc.sh`)
* **Cú pháp:** `/opt/jar-manager/scripts/focus-acc.sh <tên_acc>`
* **Ví dụ:**
  ```bash
  # Chuyển sang điều khiển acc1
  ssh vps-jar "/opt/jar-manager/scripts/focus-acc.sh acc1"

  # Chuyển sang điều khiển acc2
  ssh vps-jar "/opt/jar-manager/scripts/focus-acc.sh acc2"
  ```
* **Cơ chế hoạt động:**
  Sử dụng `xdotool` tìm ID cửa sổ dựa trên mã PID của tài khoản đó, kích hoạt nổi lên trên cùng (`windowactivate`), sau đó gọi `wmctrl` bật cờ `maximized_vert,maximized_horz` để phủ kín màn hình VNC.

### 2.3. Tắt tài khoản (`stop-acc.sh`)
* **Cú pháp:** `/opt/jar-manager/scripts/stop-acc.sh <tên_acc>`
* **Ví dụ:**
  ```bash
  ssh vps-jar "/opt/jar-manager/scripts/stop-acc.sh acc1"
  ```
* **Cơ chế hoạt động:**
  Đọc file `.pid` của tài khoản, gửi tín hiệu ngắt nhẹ nhàng (`kill`), nếu sau 1 giây tiến trình chưa dừng sẽ buộc tắt (`kill -9`), sau đó dọn dẹp file PID.

### 2.4. Kiểm tra danh sách tài khoản (`status-all.sh`)
* **Cú pháp:** `/opt/jar-manager/scripts/status-all.sh`
* **Kết quả trả về mẫu:**
  ```plaintext
  ACC:acc1|RUNNING|12823
  ACC:acc2|RUNNING|12904
  ACC:acc3|STOPPED|
  ```

### 2.5. Kiểm tra tài nguyên VPS (`system-info.sh`)
* **Cú pháp:** `/opt/jar-manager/scripts/system-info.sh`
* **Kết quả trả về mẫu:**
  ```plaintext
  RAM_TOTAL:957
  RAM_USED:177
  RAM_FREE:291
  SWAP_TOTAL:3803
  SWAP_USED:0
  CPU_LOAD:0.05
  ```

---

## 🎮 3. Hướng Dẫn Thêm Bản Game Hoặc Bản Mod Mới

Khi bạn muốn treo một bản game khác (hoặc bản mod tính năng mới):

1. **Copy file game từ máy tính lên VPS:**
   Trên PowerShell máy tính, đứng tại thư mục chứa file `.jar` mới, gõ:
   ```powershell
   scp -P 10653 ten_file_mod.jar root@remote.io.vn:/opt/jar-manager/games/
   ```
2. **Khởi chạy tài khoản với bản game mới:**
   ```bash
   ssh vps-jar "/opt/jar-manager/scripts/start-acc.sh acc5 /opt/jar-manager/games/ten_file_mod.jar"
   ```
3. **Đổi bản game mặc định cho toàn bộ acc:**
   ```bash
   ssh vps-jar "ln -sf /opt/jar-manager/games/ten_file_mod.jar /opt/jar-manager/games/game.jar"
   ```

---

## 🔧 4. Quản Lý Dịch Vụ Màn Hình Ảo (TigerVNC) & Chống Lag (Zero-Lag Engine)

* **Tối ưu độ phân giải chống lag:**
  - VNC Server chạy cố định ở `800x600 -depth 16` (thay vì 1280x800). Giảm hơn 50% lượng dữ liệu truyền qua internet và giảm 60% CPU encode trên VPS.
  - MicroEmulator chạy tỉ lệ `--resizableDevice 800 520` phủ kín màn hình, chữ to rõ, nhân vật và bảng menu to đẹp (không bị teo nhỏ như ở 1280x800).
* **Thanh tác vụ chuyển Acc siêu nhẹ (`tint2`):**
  - Đã tích hợp thanh taskbar `tint2` ở cạnh dưới màn hình (chỉ tốn ~3MB RAM).
  - Có sẵn các nút tab `[acc1]` và `[acc2]`. Click chuột vào tab là cửa sổ acc đó lập tức nổi lên full màn hình.
* **Tham số VNC Viewer chống giật lag tối đa:**
  `localhost:5901 -PreferredEncoding=Tight -QualityLevel=6 -CompressLevel=2 -AutoSelect=0 -CustomCompressLevel=1`
* **Xem trạng thái VNC:**
  ```bash
  ssh vps-jar "systemctl status vncserver.service"
  ```
* **Khởi động lại VNC:**
  ```bash
  ssh vps-jar "systemctl restart vncserver.service"
  ```
* **Xử lý sự cố VNC bị kẹt file Lock sau khi tắt đột ngột:**
  ```bash
  ssh vps-jar "rm -f /tmp/.X1-lock /tmp/.X11-unix/X1 && systemctl restart vncserver.service"
  ```
