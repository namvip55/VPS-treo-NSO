#!/bin/bash
# ==============================================================================
# SETUP-IOS-WEB.SH: TRIỂN KHAI GIAO DIỆN WEB MOBILE & NGROK STATIC TUNNEL (CHO IPHONE)
# Tự động cài noVNC, websockify, bàn phím ảo D-Pad cảm ứng và Ngrok Permanent Domain
# ==============================================================================

set -e

NGROK_TOKEN="${NGROK_TOKEN:-${NGROK_AUTHTOKEN:-}}"
NGROK_DOMAIN="${NGROK_DOMAIN:-}"
CLIENT_PIN="${CLIENT_PIN:-1234}"
GAME_TITLE="${GAME_TITLE:-JAVA GAME}"
SRC_WEB_DIR="/opt/jar-manager/web"
PROJECT_WEB_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../web" && pwd)"

# Đọc từ file .env nếu có sẵn trong thư mục
if [ -f "/opt/jar-manager/.env" ]; then
  # shellcheck disable=SC1091
  source "/opt/jar-manager/.env"
  NGROK_TOKEN="${NGROK_TOKEN:-${NGROK_AUTHTOKEN:-}}"
  NGROK_DOMAIN="${NGROK_DOMAIN:-}"
  CLIENT_PIN="${CLIENT_PIN:-${PIN:-1234}}"
elif [ -f "$(dirname "${BASH_SOURCE[0]}")/../.env" ]; then
  # shellcheck disable=SC1091
  source "$(dirname "${BASH_SOURCE[0]}")/../.env"
  NGROK_TOKEN="${NGROK_TOKEN:-${NGROK_AUTHTOKEN:-}}"
  NGROK_DOMAIN="${NGROK_DOMAIN:-}"
  CLIENT_PIN="${CLIENT_PIN:-${PIN:-1234}}"
fi

# Đọc tham số dòng lệnh
while [[ $# -gt 0 ]]; do
  case $1 in
    --token)
      NGROK_TOKEN="$2"
      shift 2
      ;;
    --domain)
      NGROK_DOMAIN="$2"
      shift 2
      ;;
    --pin)
      CLIENT_PIN="$2"
      shift 2
      ;;
    --title)
      GAME_TITLE="$2"
      shift 2
      ;;
    *)
      echo "Tham so khong hop le: $1"
      echo "Cach dung: $0 [--token <TOKEN>] [--domain <DOMAIN>] [--pin <MÃ_PIN>] [--title <TÊN_GAME>]"
      exit 1
      ;;
  esac
done

# Nếu vẫn chưa có, hỏi tương tác ẩn ký tự (không lộ màn hình)
if [ -t 0 ] && [ -z "$NGROK_TOKEN" ]; then
  echo -n "Nhập Ngrok Authtoken (ẩn ký tự): "
  read -r -s NGROK_TOKEN
  echo ""
fi

if [ -t 0 ] && [ -z "$NGROK_DOMAIN" ]; then
  echo -n "Nhập Ngrok Domain (ví dụ: mistily-stipular-kristopher.ngrok-free.dev): "
  read -r NGROK_DOMAIN
fi

echo "=================================================================="
echo "    BẮT ĐẦU CÀI ĐẶT NHÁNH WEB MOBILE & TUNNEL CHO IPHONE / IPAD"
echo "=================================================================="

# 1. Cài đặt các gói phụ thuộc (novnc, websockify, curl, tar)
echo "[1/5] Dang kiem tra va cai dat cac goi phu thuoc (novnc, websockify)..."
apt-get update -y >/dev/null 2>&1 || true
apt-get install -y novnc websockify curl tar >/dev/null 2>&1

# 2. Chuẩn bị thư mục /opt/jar-manager/web
echo "[2/5] Dong bo giao dien Web Mobile & Virtual Gamepad vao /opt/jar-manager/web..."
mkdir -p "$SRC_WEB_DIR"

# Copy core noVNC từ hệ thống vào thư mục web nếu chưa có
if [ -d "/usr/share/novnc/core" ] && [ ! -d "$SRC_WEB_DIR/core" ]; then
  cp -r /usr/share/novnc/core "$SRC_WEB_DIR/core"
fi

# Đồng bộ các file giao diện tùy biến (index.html, style.css, gamepad.js, manifest.json)
if [ -d "$PROJECT_WEB_DIR" ]; then
  cp -f "$PROJECT_WEB_DIR"/* "$SRC_WEB_DIR/" 2>/dev/null || true
fi

# Thiết lập file config.js với mã PIN riêng biệt cho VPS khách này
cat << EOF > "$SRC_WEB_DIR/config.js"
window.CONFIG = {
  title: "${GAME_TITLE}",
  account: "acc1",
  pin: "${CLIENT_PIN}"
};
EOF

# 3. Tạo dịch vụ systemd cho noVNC / websockify (Port 6080 -> VNC Display :1 Port 5901)
echo "[3/5] Cau hinh dich vu novnc.service (Websockify)..."
cat << 'EOF' > /etc/systemd/system/novnc.service
[Unit]
Description=noVNC WebSockets Bridge for Mobile Safari
After=network.target vncserver.service

[Service]
Type=simple
User=root
ExecStart=/usr/bin/websockify --web /opt/jar-manager/web 6080 localhost:5901
Restart=always
RestartSec=3

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable --now novnc.service

# 4. Cài đặt Ngrok Client nếu chưa có
echo "[4/5] Kiem tra va cai dat ngrok v3 binary..."
if ! command -v ngrok >/dev/null 2>&1; then
  echo "--> Dang tai ngrok v3 tu trang chu..."
  curl -sSL https://bin.equinox.io/c/bNyj1mQVY4c/ngrok-v3-stable-linux-amd64.tgz | tar -xz -C /usr/local/bin
  chmod +x /usr/local/bin/ngrok
fi

# 5. Cấu hình Ngrok Permanent Static Tunnel
if [ -n "$NGROK_TOKEN" ] && [ -n "$NGROK_DOMAIN" ]; then
  echo "[5/5] Cau hinh Ngrok Authtoken va Static Domain: $NGROK_DOMAIN..."
  /usr/local/bin/ngrok config add-authtoken "$NGROK_TOKEN"

  cat << EOF > /etc/systemd/system/ngrok.service
[Unit]
Description=Ngrok Permanent Tunnel for iPhone Web
After=network.target novnc.service

[Service]
Type=simple
User=root
ExecStart=/usr/local/bin/ngrok http --domain=${NGROK_DOMAIN} 6080
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
EOF

  systemctl daemon-reload
  systemctl enable --now ngrok.service

  echo "=================================================================="
  echo "✔ CÀI ĐẶT THÀNH CÔNG NHÁNH IPHONE (MÔ HÌNH 1 VPS : 1 KHÁCH)!"
  echo "👉 ĐƯỜNG LINK TRUY CẬP CHO IPHONE: https://${NGROK_DOMAIN}"
  echo "👉 MÃ PIN BẢO MẬT GIAO CHO KHÁCH:  ${CLIENT_PIN}"
  echo "👉 KHÁCH MỞ SAFARI -> BẤM 'CHIA SẺ' -> CHỌN 'THÊM VÀO MH CHÍNH'"
  echo "=================================================================="
else
  echo "[5/5] CHƯA TRUYỀN TOKEN HOẶC DOMAIN NGROK."
  echo "Dịch vụ web noVNC đã chạy sẵn tại cổng nội bộ 6080."
  echo "Khi có Token & Static Domain của Ngrok, bạn chỉ cần chạy lệnh:"
  echo "  bash /opt/jar-manager/scripts/setup-ios-web.sh --token <TOKEN> --domain <DOMAIN.ngrok-free.app>"
  echo "Hoặc chạy tạm Quick Tunnel miễn phí không cần token:"
  echo "  nohup cloudflared tunnel --url http://localhost:6080 > /tmp/tunnel.log 2>&1 &"
  echo "=================================================================="
fi
