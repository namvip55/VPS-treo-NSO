---
name: update-game
description: >-
  Cập nhật hoặc thay thế bản game .JAR trên 1 VPS cụ thể: update-game <name-vps> : thay <tab-game-cũ> = <bản jar mới>.
  Kích hoạt khi người dùng muốn đổi game, nạp bản mod mới, hoặc thay thế file game của 1 tab/slot cụ thể trên máy chủ VPS.
---

# UPDATE GAME SKILL - THAY THẾ & CẬP NHẬT GAME JAVA (.JAR) CHO TỪNG TAB/SLOT

Skill này hướng dẫn chi tiết và cung cấp các tập lệnh tự động để **thay thế bản game Java đang chạy của đúng 1 tab/slot game trên 1 VPS cụ thể** trong danh bạ hệ thống (`vps_registry.json`) mà không làm gián đoạn các tab game khác hay các VPS khác.

---

## 📌 1. CÚ PHÁP CHUẨN: `update-game <name-vps> : thay <tab-game-cũ> = <bản jar mới>`

Thao tác cập nhật game luôn phải xác định rõ **VPS đích** và **Tab/Slot game cần thay thế**:

```powershell
# Cú pháp tổng quát:
python tools/vps_manager.py update-game <name-vps> --tab <tab_cu> --jar <duong_dan_jar_moi>

# Ví dụ 1: Thay thế Tab game gốc (Tab 1 / Slot 1) trên vps1 bằng bản mod mới:
python tools/vps_manager.py update-game vps1 --tab 1 --jar "D:\games\NSO_Mod_v2.jar"

# Ví dụ 2: Thay thế Tab game khách thuê (Tab 2 / Slot 2) trên vps1 bằng game Avatar:
python tools/vps_manager.py update-game vps1 --tab 2 --jar "D:\games\Avatar250.jar"
```

---

## 🏗️ 2. NGUYÊN TẮC CÔ LẬP TAB GAME & CÁC VPS KHÁC

1. **Cô Lập Độc Lập Tab Chỉ Định:**
   * Khi thay thế **Tab 1 (Game gốc)**: Hệ thống cập nhật symlink `game.jar` và restart tiến trình `acc1`.
   * Khi thay thế **Tab 2, 3... (Slot khách)**: Hệ thống nạp file JAR mới vào `/opt/jar-manager/games/`, cập nhật cấu hình đường dẫn trong `slots.json`, và khởi động lại đúng tiến trình `slotN` trên màn hình ảo `:N`.
   * **Các tab game khác đang cày 24/7 trên VPS hoàn toàn không bị gián đoạn hay mất kết nối!**
2. **Mặc Định Điều Khiển Qua Web Phone (BẮT BUỘC):**
   * Sau khi thay thế game, khách hoặc người dùng có thể mở ngay trình duyệt Safari/Chrome trên iPhone/Android để vào kiểm tra màn hình game mới thông qua mã PIN tương ứng của tab đó.
   * **Không tự ý đóng gói EXE/ZIP:** Nếu người dùng không yêu cầu skill `package-client`, tuyệt đối không tự ý build file `NSO_Client.exe` hay ZIP. Mặc định luôn bàn giao link Web Phone.

---

## ⚡ 3. Quy Trình Cập Nhật Game Tự Động (Tools vps_manager.py)

Khi thực thi lệnh `python tools/vps_manager.py update-game <name-vps> --tab <tab_cu> --jar <file_jar>`:
1. Tự động lưu bản sao file game vào đúng thư mục `servers/<name-vps>/` trên máy tính quản trị.
2. Tự động đẩy file JAR lên VPS mục tiêu qua SCP.
3. Cập nhật symlink hoặc cấu hình `slots.json` của đúng tab/slot chỉ định.
4. Tự động restart tiến trình game của tab đó để nạp dữ liệu mới.
5. Kiểm tra trạng thái hoạt động và báo cáo link Web Phone cho người dùng.

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
