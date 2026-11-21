# ĐẶC TẢ MẠNG NAT, BẢO MẬT & XỬ LÝ SỰ CỐ (NETWORK & TROUBLESHOOTING)

Tài liệu ghi lại toàn bộ kinh nghiệm thực chiến, đặc thù mạng IP NAT của nhà cung cấp, giải pháp định tuyến và các bước khắc phục sự cố chuẩn xác nhất.

---

## 🌐 1. Đặc Thù Mạng VPS IP NAT & Cơ Chế Định Tuyến VLAN

### 1.1. Bản chất hệ thống mạng của nhà cung cấp
* **IP NAT:** VPS không sở hữu một địa chỉ IPv4 công cộng riêng. Nhà cung cấp sử dụng router trung tâm để ánh xạ cổng:
  * Bên ngoài: `remote.io.vn:10653`
  * Bên trong VPS: `10.50.2.141:22` (giao diện mạng `eth0.50`)
* **Lệnh `changeip` (Bắt buộc sau mỗi lần cài lại VPS):**
  * Khi VPS vừa khởi tạo xong, card mạng internet chưa được gán.
  * Lệnh `/usr/local/bin/changeip` là script nội bộ của nhà mạng, tự động kết nối tới gateway VLAN, cấp dải IP mạng (FPT/VNPT/Viettel) qua `dhclient` và thiết lập bảng định tuyến `table ssh_rt` (rule 200).
  * **Cách cấp mạng tự động không cần bấm tay:**
    ```bash
    echo -e "1\ny" | /usr/local/bin/changeip
    ```

---

## ⚠️ 2. Bài Học Xương Máu: TUYỆT ĐỐI KHÔNG BẬT UFW

### Nguyên nhân kỹ thuật gây mất kết nối SSH:
* Trong các VPS thông thường, bật `ufw allow 22/tcp` là tiêu chuẩn. Nhưng **trên VPS IP NAT này, bật UFW sẽ làm mất kết nối SSH ngay lập tức**.
* **Lý do:** Gói tin SSH đi qua giao diện VLAN ảo `eth0.50` và bảng định tuyến chính sách `ssh_rt`. Khi UFW kích hoạt, bộ lọc `iptables` và cơ chế chống giả mạo IP (`rp_filter`) của Linux sẽ coi các gói tin phản hồi từ router NAT là bất hợp lệ và lập tức chặn luồng (drop packets).
* **An toàn thực tế:**
  1. Router phần cứng của nhà mạng đã chặn 100% các cổng từ internet, chỉ mở duy nhất cổng SSH đã ánh xạ.
  2. Màn hình ảo TigerVNC chạy với tham số `-localhost yes`, chỉ lắng nghe trên `127.0.0.1`.
  3. Vì vậy, hệ thống **an toàn tuyệt đối mà không cần bật UFW**, giúp mạng hoạt động thông suốt không bao giờ bị nghẽn.

---

## 🔑 3. Cơ Chế Xác Thực SSH Không Mật Khẩu (Ed25519)

Hệ thống sử dụng chuẩn mã hóa khóa bất đối xứng **Ed25519** hiện đại:

1. **Khóa trên máy tính Windows cá nhân:**
   * Private Key: `C:\Users\Admin\.ssh\id_ed25519_jar`
   * Public Key: `C:\Users\Admin\.ssh\id_ed25519_jar.pub`
2. **Khóa trên VPS:**
   * Lưu tại: `/root/.ssh/authorized_keys`
3. **Cấu hình Alias trong `C:\Users\Admin\.ssh\config`:**
   ```ssh-config
   Host vps-jar
       HostName remote.io.vn
       Port 10653
       User root
       IdentityFile ~/.ssh/id_ed25519_jar
       ServerAliveInterval 30
       ServerAliveCountMax 3
       StrictHostKeyChecking no
       UserKnownHostsFile /dev/null
   ```
   *Nhờ cấu hình này, mọi tập lệnh trên Windows chỉ cần gọi `vps-jar` là kết nối ngay mà không bao giờ bị hỏi mật khẩu hay bị ngắt kết nối giữa chừng (`ServerAliveInterval 30`)*.

---

## 🚨 4. Bảng Tra Cứu & Xử Lý Sự Cố Thường Gặp

### Sự cố 1: Gõ `ssh vps-jar` bị báo "Connection timed out"
* **Nguyên nhân 1:** VPS bị tắt trên giao diện web ➜ Đăng nhập web bấm **Start**.
* **Nguyên nhân 2:** Card mạng bị rớt sau khi VPS bị reboot ➜ Mở noVNC Console gõ: `echo -e "1\ny" | /usr/local/bin/changeip`.

### Sự cố 2: Báo lỗi "bind: Address already in use" khi mở SSH Tunnel cổng 5901
* **Nguyên nhân:** Phiên SSH Tunnel cũ trên máy tính của bạn vẫn đang chạy ngầm.
* **Cách xử lý trên PowerShell Windows:**
  ```powershell
  # Tìm và tắt tiến trình ssh đang giữ cổng
  Get-Process ssh -ErrorAction SilentlyContinue | Stop-Process -Force
  ```
  Sau đó chạy lại lệnh: `ssh -N -L 5901:localhost:5901 vps-jar`.

### Sự cố 3: Mở TigerVNC màn hình đen kịt hoặc không thao tác được
* **Cách xử lý:** Khởi động lại dịch vụ VNC trên VPS bằng lệnh:
  ```powershell
  ssh vps-jar "rm -f /tmp/.X1-lock /tmp/.X11-unix/X1 && systemctl restart vncserver.service"
  ```
  Sau đó bấm chuột vào cửa sổ VNC chọn **Connect** lại.

### Sự cố 5: SSH bị lỗi "Connection timed out" ngẫu nhiên do Round-Robin DNS
* **Nguyên nhân:** Tên miền `remote.io.vn` được nhà cung cấp cấu hình DNS Round-Robin trỏ về hơn 8 cụm router NAT khác nhau (`14.250.250.229`, `14.236.172.236`, `14.242.71.192`,...). Khi dùng tên miền `remote.io.vn`, mỗi lần kết nối SSH sẽ phân giải sang một IP ngẫu nhiên, nếu không đúng node chứa VPS sẽ bị timeout hoặc lỗi host key.
* **Cách xử lý:** Trong `C:\Users\Admin\.ssh\config`, thay thế `HostName remote.io.vn` bằng **địa chỉ IPv4 thực tế** của cụm node (ví dụ: `14.250.250.229`).

### Sự cố 6: Server NSO bị crash sau khi cài gói Java do đè phiên bản Java mặc định
* **Nguyên nhân:** Server NSO yêu cầu OpenJDK 17. Nếu chạy lệnh `apt-get install openjdk-8-jre`, Ubuntu sẽ đổi symlink `/usr/bin/java` về Java 8 khiến Server không thể khởi động được (`UnsupportedClassVersionError`).
* **Cách xử lý:** 
  - MicroEmulator 2.0.4 tương thích 100% với OpenJDK 17. Không cài Java 8 trên VPS có Server.
  - Trong cấu hình service `/etc/systemd/system/nso-server.service`, luôn chỉ định đường dẫn tuyệt đối:
    `/usr/lib/jvm/java-17-openjdk-amd64/bin/java`.

### Sự cố 7: Màn hình VNC bị kẹt ở màn hình "Launcher / Start" của MicroEmulator
* **Nguyên nhân:** Khi khởi chạy MicroEmulator bằng file JAR mà không chỉ định MIDlet class, MicroEmulator sẽ mở cửa sổ chọn MIDlet của nó thay vì vào thẳng game.
* **Cách xử lý:** 
  - Đã được tự động hóa trong `start-acc.sh`: Tự động đọc thuộc tính `MIDlet-1` từ `META-INF/MANIFEST.MF` của file jar (ví dụ `GameMidlet`) và truyền vào lệnh chạy:
    `setsid java ... "$MIDLET_CLASS" "$JAR"`
  - Sau đó tự động gửi phím `Return` bằng `xdotool` để vào thẳng màn hình game.
