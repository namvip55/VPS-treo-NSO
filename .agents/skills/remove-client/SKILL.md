---
name: remove-client
description: >-
  Quy trình và tập lệnh thu hồi quyền truy cập, tắt game và dọn sạch dữ liệu khi khách hàng ngừng thuê VPS hoặc ngừng thuê slot game J2ME.
  Kích hoạt khi người dùng muốn xóa khách, thu hồi quyền truy cập, xóa slot game, ngừng treo acc khách, hoặc dọn dẹp giải phóng tài nguyên VPS.
---

# REMOVE CLIENT & CLEANUP SKILL - THU HỒI & DỌN DẸP KHÁCH HÀNG

Skill này cung cấp quy trình chuẩn mực và các tập lệnh tự động để **thu hồi quyền truy cập, dừng tiến trình game và xóa sạch dữ liệu tài khoản** khi một khách hàng ngừng thuê dịch vụ treo game (hết hạn hoặc không gia hạn), giúp giải phóng tài nguyên và bảo vệ quyền riêng tư.

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
