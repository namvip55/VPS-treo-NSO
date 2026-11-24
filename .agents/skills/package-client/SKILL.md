---
name: package-client
description: >-
  Quy trình tự động đóng gói ứng dụng điều khiển 1-click giao cho khách hàng (Single EXE hoặc Portable ZIP) khi làm dịch vụ cấu hình VPS treo game J2ME (NSO).
  Kích hoạt khi người dùng muốn xuất file gửi khách, đóng gói app cho khách, tạo bộ cài cho khách, hoặc bàn giao VPS cho người chơi.
---

# PACKAGE CLIENT SKILL - ĐÓNG GÓI BỘ ĐIỀU KHIỂN BÀN GIAO CHO KHÁCH HÀNG (1-CLICK)

> [!IMPORTANT]
> **ĐIỀU KIỆN KÍCH HOẠT:**
> Skill này **CHỈ ĐƯỢC THỰC HIỆN KHI NGƯỜI DÙNG YÊU CẦU ĐÍCH DANH** (*"đóng gói app cho khách", "xuất file exe", "tạo bộ cài cho khách", "package-client"*).
> **Mặc định:** Toàn bộ hệ thống giao diện điều khiển bàn giao cho khách là **Web Mobile Phone trên Safari / Chrome** (noVNC + Virtual Gamepad + mã PIN riêng). Tuyệt đối không tự ý đóng gói EXE/ZIP khi người dùng không yêu cầu.

Khi bạn nhận dịch vụ cấu hình VPS treo game cho khách hàng có nhu cầu điều khiển trên máy tính Windows, khách thường là người chơi bình thường, **không có kiến thức về SSH, Linux hay Terminal**.

Skill này cung cấp quy trình tự động hóa 100% để bạn tạo ra **1 file EXE duy nhất (hoặc 1 file ZIP Portable)** gửi cho khách qua Zalo/Drive. Khách chỉ cần tải về, nhấp đúp chuột là vào thẳng game cày 24/7!

---

## 📦 1. Hai Lựa Chọn Đóng Gói Bàn Giao

### 🌟 Lựa chọn 1: Single Standalone EXE (Khuyên Dùng Tuyệt Đối)
* **Quy cách:** Xuất ra đúng **1 file `.exe` duy nhất** (khoảng ~23MB).
* **Đặc điểm kỹ thuật:**
  - Nhúng sẵn `vncviewer.exe` (TigerVNC Viewer) bên trong tài nguyên file EXE.
  - Nhúng sẵn khóa SSH bí mật và thông số kết nối (IP VPS, Port NAT).
  - Tự động chạy SSH Tunnel ngầm qua cổng 5901 và mở màn hình game.
  - Tự động dọn dẹp tiến trình khi khách tắt cửa sổ.
* **Trải nghiệm của khách:** Khách không cần giải nén, để file ngoài Desktop nhấp đúp là màn hình game hiện lên ngay lập tức!

### 📁 Lựa chọn 2: Gói Thư Mục Portable ZIP
* **Quy cách:** Xuất ra file `.zip` siêu nhẹ (~7.6MB).
* **Bên trong gồm:**
  - `[Tên_Game].exe` (Launcher điều khiển WinForms).
  - `vncviewer.exe` (TigerVNC Viewer Portable).
  - `client.key` (Khóa SSH riêng của khách).
  - `HUONG_DAN_SU_DUNG.txt` (Hướng dẫn chi tiết).
* **Trải nghiệm của khách:** Khách giải nén thư mục ra Desktop, bấm file `.exe` để chơi.

---

## ⚡ 2. Hướng Dẫn Sử Dụng Script Đóng Gói (`tools/package_client.py`)

Công cụ đóng gói nằm tại [tools/package_client.py](file:///d:/project/Vps-Treo-Nso/tools/package_client.py).

### 2.1. Đóng gói 1 file EXE duy nhất gửi khách (Mặc định)
```powershell
# Cú pháp cơ bản:
python tools/package_client.py --host <IP_VPS_KHACH> --port <PORT_NAT_KHACH> --title "<TEN_GAME_HOAC_KHACH>"

# Ví dụ thực tế:
python tools/package_client.py --host 14.250.250.229 --port 12724 --title "NSO GEMINI"
```
*Kết quả:* File được xuất tại `dist_clients/NSO_GEMINI_Client.exe`. Bạn chỉ cần gửi file này cho khách!

---

### 2.2. Đóng gói dạng ZIP Portable
```powershell
python tools/package_client.py --host 14.250.250.229 --port 12724 --title "NSO GEMINI" --mode zip
```
*Kết quả:* File được xuất tại `dist_clients/NSO_GEMINI_Portable.zip`.

---

### 2.3. Cấp khóa SSH Ed25519 riêng biệt cho từng khách hàng
Để tăng cường bảo mật (khách này không dùng chung khóa với khách khác):
1. **Sinh cặp khóa riêng cho khách:**
   ```powershell
   ssh-keygen -t ed25519 -N "" -f "tools/keys/khach_hung.key" -C "khach-hung"
   ```
2. **Nạp public key vào VPS của khách:**
   ```powershell
   type tools\keys\khach_hung.key.pub | ssh -p <PORT_NAT> root@<IP_VPS> "cat >> /root/.ssh/authorized_keys"
   ```
3. **Đóng gói file EXE dùng khóa riêng đó:**
   ```powershell
   python tools/package_client.py --host <IP_VPS> --port <PORT_NAT> --title "NSO Khach Hung" --key "tools/keys/khach_hung.key"
   ```

---

## 🖥️ 3. Cơ Chế Hoạt Động Của Ứng Dụng Client Trên Máy Khách

1. **Giao diện Windows thuần túy (Pure C# WinForms .NET 4.0):**
   - 100% các máy tính chạy Windows 10 và Windows 11 đều có sẵn .NET Framework 4.0/4.5. Khách không cần cài thêm Python, Node.js hay Visual Studio.
   - Không phụ thuộc vào PowerShell rời, tránh bị lỗi Policy Script hay hiện màn hình đen console.
2. **Quản lý phiên VNC & SSH Tunnel tự động:**
   - Khi bấm **`▶ VAO THAO TAC GAME (1 CLICK)`**: Tool kiểm tra cổng nội bộ 5901. Nếu chưa có tunnel, tool tự kích hoạt `ssh.exe` ngầm kết nối bảo mật tới VPS.
   - Khởi chạy VNC Viewer với cờ tối ưu triệt tiêu giật lag:
     `localhost:5901 -PreferredEncoding=Tight -QualityLevel=6 -CompressLevel=1 -AutoSelect=0 -CustomCompressLevel=1`
3. **Tự động ngắt kết nối an toàn:**
   - Khi khách tắt ứng dụng, tool tự giải phóng tunnel ngầm để giải phóng tài nguyên mạng cho máy khách.

---

## 📋 4. Mẫu Tin Nhắn Hướng Dẫn Gửi Kèm Cho Khách Hàng

Khi gửi file cho khách qua Zalo/Facebook/Telegram, bạn chỉ cần copy mẫu tin nhắn sau:

```text
Chào bạn! Mình đã cấu hình xong toàn bộ hệ thống VPS treo game 24/7 cho bạn.

🎮 BẠN CHỈ CẦN THAO TÁC 2 BƯỚC ĐỂ VÀO GAME:
1. Tải file đính kèm về máy tính (để ngoài màn hình Desktop cho tiện).
2. Nhấp đúp vào file, sau đó bấm nút lớn: "▶ VÀO THAO TÁC GAME (1 CLICK)".

✨ LƯU Ý KHI SỬ DỤNG:
- Bàn phím số NumPad đã chuẩn hóa (Phím 8 nhảy, phím 2 cúi, phím 4/6 đi, phím 5 đánh, 7/9/1/3 skill).
- Thao tác cài auto hoặc chọn khu xong, bạn chỉ cần bấm dấu [X] tắt cửa sổ game đi.
- Game trên VPS vẫn tự động cày ngầm liên tục 24/7 bình thường!
```
