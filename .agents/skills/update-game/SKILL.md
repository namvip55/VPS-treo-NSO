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

## ⚡ 2. Quy Trình Cập Nhật Game Trong 10 Giây

### Bước 1: Tải file game `.jar` mới lên VPS
Từ máy tính cá nhân (PowerShell hoặc Command Prompt), đứng tại thư mục chứa file game mới:

```powershell
# Ví dụ file game mới tên là: Avatar250.jar
scp Avatar250.jar vps-jar:/opt/jar-manager/games/
```
*(Hoặc dùng WinSCP kéo file game vào thư mục `/opt/jar-manager/games/`).*

### Bước 2: Trỏ Symlink sang game mới
Chạy lệnh sau qua SSH:

```powershell
ssh vps-jar "ln -sf /opt/jar-manager/games/Avatar250.jar /opt/jar-manager/games/game.jar"
```

### Bước 3: Khởi động lại game
* **Cách 1 (Dùng app):** Mở [JarManager.exe](file:///C:/JarManager/JarManager.exe) ngoài Desktop ➜ Bấm nút **`↺ Khoi Dong Lai Game`**.
* **Cách 2 (Dùng lệnh):**
  ```powershell
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
