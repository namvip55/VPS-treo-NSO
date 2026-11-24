---
name: remove-client
description: >-
  Thu hồi quyền truy cập và xóa tab game của khách thuê: remove-client <name-vps> <tab-game-khách>.
  Kích hoạt khi khách hàng ngừng thuê slot, cần xóa tab game, thu hồi mã PIN, dừng game và dọn sạch dữ liệu RMS của khách trên 1 VPS cụ thể.
---

# REMOVE CLIENT SKILL - THU HỒI & DỌN DẸP TAB GAME KHÁCH THUÊ

Skill này cung cấp quy trình chuẩn mực và tập lệnh tự động để **thu hồi quyền truy cập, dừng tiến trình game và xóa sạch dữ liệu tài khoản** khi một khách hàng ngừng thuê dịch vụ (hết hạn hoặc không gia hạn).

---

## 📌 1. CÚ PHÁP CHUẨN: `remove-client <name-vps> <tab-game-khách>`

Thao tác thu hồi **BẮT BUỘC** phải trỏ đúng tên VPS và đúng tab/slot game khách thuê:

```powershell
# Cú pháp tổng quát:
python tools/vps_manager.py remove-client <name-vps> <tab-game-khách>

# Ví dụ 1: Xóa tab game khách thuê ở Slot 2 trên máy chủ vps1:
python tools/vps_manager.py remove-client vps1 slot2
# hoặc:
python tools/vps_manager.py remove-client vps1 2

# Ví dụ 2: Xóa slot 3 trên VPS khách mẫu vps2_sample:
python tools/vps_manager.py remove-client vps2_sample 3
```

---

## 🛡️ 2. NGUYÊN TẮC BẢO VỆ & CÔ LẬP TUYỆT ĐỐI

1. **Trỏ đúng Tab Game Khách Thuê:**
   * Hệ thống chỉ tắt màn hình ảo VNC `:N`, tiến trình game `slotN`, và thư mục `/opt/jar-manager/profiles/slotN/`.
   * **Slot 1 (Game gốc của chủ VPS) và các tab của khách khác được bảo vệ nguyên vẹn 100%.**
2. **Thu Hồi Quyền Truy Cập Ngay Lập Tức:**
   * Mã PIN của khách bị xóa vĩnh viễn khỏi file cấu hình định tuyến `/opt/jar-manager/web/tokens.cfg`.
   * Khách cũ mở link Web Phone Safari/Chrome nhập mã PIN cũ sẽ bị từ chối ngay lập tức!
3. **Bảo Mật Dữ Liệu RMS:**
   * Dữ liệu tài khoản, mật khẩu game của khách trong thư mục RMS được dọn sạch hoàn toàn, không lưu lại dấu vết.
4. **Giải Phóng Bộ Nhớ RAM:**
   * Trả lại ngay lập tức ~70-80MB RAM cho VPS để sẵn sàng cấp phát cho khách hàng tiếp theo.

---

## 🏗️ 1. HAI TRƯỜNG HỢP THU HỒI THỰC TẾ

Tùy theo mô hình mà khách hàng đang sử dụng, bạn chọn đúng quy trình tương ứng:

```text
                  +----------------------------------+
                  |  KHÁCH HÀNG NGỪNG THUÊ DỊCH VỤ   |
                  +-----------------+----------------+
                                    |
            +-----------------------+-----------------------+
            |                                               |
            v                                               v
[TRƯỜNG HỢP 1: THUÊ THEO SLOT]            [TRƯỜNG HỢP 2: THUÊ RIÊNG NGUYÊN VPS]
(Chung 1 VPS & 1 Link Ngrok)              (Có VPS riêng & File EXE riêng)
  1. Tắt game & VNC Display :N              1. Thu hồi SSH Key trong authorized_keys
  2. Thu hồi mã PIN trong tokens.cfg        2. Dừng game & xóa dữ liệu profiles/<acc>
  3. Xóa sạch RMS profiles/slotN            3. Xóa key trong dist_clients/
  4. Giải phóng RAM cho khách mới           4. Sẵn sàng cấu hình lại cho khách mới
```

---

## ⚡ 2. QUY TRÌNH XỬ LÝ CHI TIẾT

### 2.1. Trường Hợp 1: Khách Thuê Theo Slot (Chia Sẻ VPS & 1 Link Ngrok)

Khi khách hàng thuê Slot 2 (hoặc Slot 3, 4...) hết hạn:

#### Cách A: Chạy trực tiếp qua công cụ quản lý máy tính (Khuyên Dùng)
```powershell
# Xóa slot 2 trên VPS chỉ định (ví dụ: vps1):
python tools/vps_manager.py remove-slot --vps vps1 --slot 2
```

#### Cách B: Chạy lệnh trực tiếp trên terminal VPS
```bash
# Xóa hoàn toàn Slot 2:
bash /opt/jar-manager/scripts/slot-manager.sh remove 2
```

**Cơ chế tự động thực hiện:**
1. Dừng ngay tiến trình Java game của slot đó (`stop-acc.sh slot2`).
2. Tắt màn hình ảo VNC Display tương ứng (`vncserver -kill :2`).
3. **Thu hồi mã PIN:** Xóa dòng mapping của khách trong `/opt/jar-manager/web/tokens.cfg` $\rightarrow$ Khách cũ vào web gõ PIN cũ sẽ bị từ chối truy cập ngay lập tức!
4. **Bảo mật dữ liệu:** Xóa sạch thư mục `/opt/jar-manager/profiles/slot2/` (xóa mật khẩu game lưu trong RMS).
5. **Giải phóng RAM:** Khôi phục lại ~80MB RAM cho VPS, slot 2 trở về trạng thái trống sẵn sàng đón khách tiếp theo.

---

### 2.2. Trường Hợp 2: Khách Thuê Riêng Nguyên 1 VPS (Dùng File EXE & Khóa SSH)

Khi khách thuê nguyên máy chủ VPS ngừng gia hạn:

#### Bước 1: Thu hồi Khóa SSH của khách trên VPS
Khóa của khách được lưu trong `/root/.ssh/authorized_keys` kèm theo wrapper. Để tước quyền:
```bash
# Kiểm tra các khóa đang có:
grep "client-cmd-wrapper.sh" /root/.ssh/authorized_keys

# Xóa khóa của khách hàng (chỉ giữ lại khóa quản trị id_ed25519_jar của bạn):
# Cách đơn giản: Mở file xóa dòng chứa "client-cmd-wrapper.sh" của khách đó:
nano /root/.ssh/authorized_keys
```
*Ngay khi xóa dòng này, file `NSO_Client.exe` trên máy tính của khách cũ sẽ bị ngắt kết nối vĩnh viễn (Permission denied), không thể mở tunnel vào VPS được nữa.*

#### Bước 2: Dừng game và dọn sạch dữ liệu cũ
```bash
# Dừng game:
/opt/jar-manager/scripts/stop-acc.sh acc1

# Xóa dữ liệu tài khoản cũ để bảo mật:
rm -rf /opt/jar-manager/profiles/acc1
```

#### Bước 3: Dọn dẹp trên máy tính quản trị
Xóa file đóng gói cũ và key của khách trong thư mục cục bộ:
```powershell
# Xóa bộ cài đã xuất giao khách:
Remove-Item "dist_clients\client_<host>_<port>.key*" -Force
```

---

## 🔍 3. BẢNG KIỂM TRA AN TOÀN SAU KHI THU HỒI (CHECKLIST)

Sau khi xóa khách, kiểm tra lại để đảm bảo máy chủ sạch sẽ:

1. **Kiểm tra tiến trình:**
   ```bash
   bash /opt/jar-manager/scripts/slot-manager.sh list
   # hoặc:
   ps aux | grep java
   ```
   *(Xác nhận slot cũ không còn chạy ngầm ngốn RAM).*

2. **Kiểm tra bảng mã PIN Web:**
   ```bash
   cat /opt/jar-manager/web/tokens.cfg
   ```
   *(Xác nhận mã PIN của khách cũ đã biến mất).*

3. **Kiểm tra RAM đã được hoàn trả:**
   ```bash
   bash /opt/jar-manager/scripts/system-info.sh
   ```
   *(RAM khả dụng tăng lên, sẵn sàng cho khách hàng mới).*
