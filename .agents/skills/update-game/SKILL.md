---
name: update-game
description: >-
  Quy trình và tập lệnh cập nhật hoặc thay thế bản game J2ME (.jar) trên VPS treo game 24/7 (Ninja School, Avatar, Ngọc Rồng, Hiệp Sĩ Online...).
  Kích hoạt khi người dùng muốn đổi game, nạp bản mod mới, hoặc chuyển đổi file game .jar đang chạy.
---

# UPDATE GAME SKILL - THAY THẾ & CẬP NHẬT GAME JAVA (.JAR)

Skill này hướng dẫn chi tiết và cung cấp các tập lệnh tự động để thay thế tựa game Java đang chạy trên VPS (từ Ninja School Online sang game khác hoặc nạp bản mod mới) mà không làm gián đoạn hay phải cài đặt lại hệ điều hành VPS.

---

## 🏗️ 1. Kiến Trúc Quản Lý File Game Trên VPS

Hệ thống sử dụng cơ chế **Symlink (Liên kết tượng trưng)** để tách rời bộ giả lập và file game:

```text
/opt/jar-manager/games/
├── HoiUc220.jar                 # Bản game gốc (ví dụ NSO Hồi Ức 220)
├── Avatar250.jar                # Bản game khác (ví dụ Avatar)
├── HiepSiOnline.jar             # Bản game khác (ví dụ Hiệp Sĩ)
└── game.jar -> HoiUc220.jar     # SYMLINK: Luôn trỏ vào bản game đang chạy mặc định
```

* File script khởi động `/opt/jar-manager/scripts/start-acc.sh` luôn gọi đường dẫn:
  `/opt/jar-manager/games/game.jar`
* Khi bạn muốn đổi sang bất kỳ game nào, chỉ cần **trỏ symlink `game.jar` sang file game mới**, toàn bộ hệ thống từ VNC đến `JarManager` sẽ tự động chuyển sang game mới!

---

## ⚡ 2. Quy Trình Cập Nhật Game Trong 10 Giây (Hỗ Trợ Multi-VPS)

### Cách 1: Dùng Tool Quản Lý Tập Trung (Khuyên Dùng - Tự Động 100%, Không Lẫn VPS)
Khi quản lý nhiều VPS, hãy dùng công cụ [tools/vps_manager.py](file:///d:/project/Vps-Treo-Nso/tools/vps_manager.py) để chỉ định chính xác VPS cần update:

```powershell
# Cập nhật game cho đúng VPS chỉ định (ví dụ: vps1):
python tools/vps_manager.py update-game --vps vps1 --jar "path/to/game_moi.jar"

# Hoặc nếu file game đã được đặt trong servers/vps1/, chỉ cần gõ:
python tools/vps_manager.py update-game --vps vps1
```
* **Lợi ích:**
  - Tự động copy file vào thư mục riêng `servers/<vps_id>/` trên máy tính.
  - Tự động đẩy file JAR lên đúng VPS đó.
  - Tự động cập nhật symlink `game.jar` và restart acc trên VPS đó.
  - **Hoàn toàn không ảnh hưởng đến các VPS khác trong hệ thống!**

---

### Cách 2: Thao Tác Bằng Dòng Lệnh Thủ Công
Từ máy tính cá nhân (PowerShell hoặc Command Prompt), đứng tại thư mục chứa file game mới:

```powershell
# Bước 1: Tải file game .jar mới lên VPS
scp Avatar250.jar vps-jar:/opt/jar-manager/games/

# Bước 2: Trỏ Symlink sang game mới
ssh vps-jar "ln -sf /opt/jar-manager/games/Avatar250.jar /opt/jar-manager/games/game.jar"

# Bước 3: Khởi động lại game
ssh vps-jar "/opt/jar-manager/scripts/stop-acc.sh acc1 && sleep 1 && /opt/jar-manager/scripts/start-acc.sh acc1"
```

---

## 💾 3. Xử Lý Dữ Liệu Tài Khoản Cũ (RMS Data)

* Mỗi tài khoản lưu dữ liệu cài đặt, tài khoản, mật khẩu trong:
  `/opt/jar-manager/profiles/<tên_acc>/`
* **Khi đổi sang một game hoàn toàn khác:**
  Nên xóa dữ liệu RMS cũ để tránh xung đột cấu hình giữa 2 tựa game khác nhau:
  ```powershell
  ssh vps-jar "rm -rf /opt/jar-manager/profiles/acc1/.microemulator"
  ```
  *(Khi khởi động lại, game mới sẽ tạo bộ nhớ lưu trữ sạch 100%).*

---

## 🎮 4. Kiểm Tra Tương Thích & Đồ Họa Của Game Mới

1. **Độ phân giải màn hình:**
   * Màn hình VNC đang là chuẩn **800x600**.
   * Hầu hết game J2ME trên thị trường (Avatar, NSO, Hiệp Sĩ, Ngọc Rồng) đều tự động co giãn theo kích thước Canvas 800x550.
2. **Kiểm tra chụp ảnh màn hình:**
   ```powershell
   ssh vps-jar "DISPLAY=:1 scrot /tmp/check_game.png"
   scp vps-jar:/tmp/check_game.png ./check_game.png
   ```
3. **Mở VNC thao tác thử:**
   Bấm nút **`▶ VAO THAO TAC GAME (1 CLICK)`** trên `JarManager.exe` để đăng nhập nick và chơi thử.
