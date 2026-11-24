#!/bin/bash
# ==============================================================================
# SLOT-MANAGER.SH: QUẢN LÝ ĐA GAME / ĐA KHÁCH TRÊN 1 VPS & 1 LINK NGROK DUY NHẤT
# Tự động cấp màn hình ảo VNC riêng, cô lập dữ liệu và định tuyến bằng mã PIN
# ==============================================================================

set -e

ACTION="${1:-list}"
SLOT_FILE="/opt/jar-manager/slots.json"
TOKENS_CFG="/opt/jar-manager/web/tokens.cfg"
WEB_CONFIG="/opt/jar-manager/web/config.js"

mkdir -p /opt/jar-manager/profiles /opt/jar-manager/web

# Đảm bảo file slots.json tồn tại
if [ ! -f "$SLOT_FILE" ]; then
    echo "{}" > "$SLOT_FILE"
fi

case "$ACTION" in
    add)
        SLOT_ID="$2"
        JAR_PATH="$3"
        PIN="$4"
        TITLE="${5:-JAVA GAME}"

        if [ -z "$SLOT_ID" ] || [ -z "$JAR_PATH" ] || [ -z "$PIN" ]; then
            echo "Thieu tham so!"
            echo "Cach dung: $0 add <slot_number> <duong_dan_file.jar> <ma_pin> [tieu_de_game]"
            echo "Vi du:     $0 add 2 /opt/jar-manager/games/Avatar.jar 2222 'Avatar 250'"
            exit 1
        fi

        if [ ! -f "$JAR_PATH" ]; then
            echo "Loi: File game khong ton tai tai: $JAR_PATH"
            exit 1
        fi

        VNC_PORT=$((5900 + SLOT_ID))
        ACC_NAME="slot${SLOT_ID}"

        echo "=== [1/4] KHOI TAO MAN HINH AO VNC :${SLOT_ID} (PORT ${VNC_PORT}) ==="
        # Kiem tra neu display da chay, neu chua thi bat len
        if ! pgrep -f "Xtigervnc :${SLOT_ID}" > /dev/null 2>&1; then
            /usr/bin/vncserver -kill ":${SLOT_ID}" >/dev/null 2>&1 || true
            /usr/bin/vncserver ":${SLOT_ID}" -geometry 800x600 -depth 16 -localhost yes -SecurityTypes None
        fi

        echo "=== [2/4] KHOI CHAY GAME ${TITLE} CHO ACC ${ACC_NAME} ==="
        /opt/jar-manager/scripts/start-acc.sh "$ACC_NAME" "$JAR_PATH" "$SLOT_ID"

        echo "=== [3/4] CAP NHAT ROUTING NGROK TOKENS (MA PIN -> PORT ${VNC_PORT}) ==="
        # Xoa token cu neu co va them token moi
        touch "$TOKENS_CFG"
        sed -i "/^${PIN}:/d" "$TOKENS_CFG" 2>/dev/null || true
        echo "${PIN}: 127.0.0.1:${VNC_PORT}" >> "$TOKENS_CFG"

        # Luu thong tin slot vao slots.json
        python3 -c "
import json
try:
    with open('$SLOT_FILE', 'r') as f:
        data = json.load(f)
except Exception:
    data = {}
data['$SLOT_ID'] = {
    'slot': $SLOT_ID,
    'acc': '$ACC_NAME',
    'game': '$TITLE',
    'jar': '$JAR_PATH',
    'pin': '$PIN',
    'port': $VNC_PORT
}
with open('$SLOT_FILE', 'w') as f:
    json.dump(data, f, indent=2)
"

        # Cap nhat file config.js tren web
        cat << 'EOF_PY' > /tmp/gen_config.py
import json, os
SLOT_FILE = "/opt/jar-manager/slots.json"
WEB_CONFIG = "/opt/jar-manager/web/config.js"
data = {}
if os.path.exists(SLOT_FILE):
    try:
        with open(SLOT_FILE) as f: data = json.load(f)
    except: pass

lines = ["// Tu dong sinh boi slot-manager.sh", "window.SLOTS = {"]
for sid, item in data.items():
    lines.append(f"  \"{item['pin']}\": {{ acc: \"{item['acc']}\", game: \"{item['game']}\", slot: {item['slot']}, port: {item['port']} }},")
lines.append("};")
lines.append("window.CONFIG = { title: 'MULTI-GAME CLOUD', default_pin: '1234' };")
with open(WEB_CONFIG, "w") as f:
    f.write("\n".join(lines) + "\n")
EOF_PY
        python3 /tmp/gen_config.py && rm -f /tmp/gen_config.py

        echo "=== [4/4] BÀN GIAO CHO KHÁCH HÀNG MỚI THÀNH CÔNG ==="
        echo "=================================================================="
        echo "👉 TỰA GAME:     ${TITLE}"
        echo "👉 MÃ PIN RIÊNG:  ${PIN}"
        echo "👉 MÀN HÌNH VNC:  Display :${SLOT_ID} (Port nội bộ ${VNC_PORT})"
        echo "👉 BÀN GIAO CHO KHÁCH LINK: [LINK_NGROK_CỦA_BẠN]/?pin=${PIN}"
        echo "   (Khách chỉ cần bấm link hoặc nhập PIN ${PIN} là vào thẳng game riêng!)"
        echo "=================================================================="
        ;;

    list)
        echo "================================================================================"
        echo "DANH SÁCH CÁC SLOT GAME ĐANG TREO TRÊN VPS (CHIA SẺ 1 LINK NGROK)"
        echo "================================================================================"
        printf "%-6s | %-16s | %-10s | %-6s | %-8s | %s\n" "SLOT" "TỰA GAME" "ACC" "PIN" "PORT" "TRẠNG THÁI"
        echo "--------------------------------------------------------------------------------"
        python3 -c "
import json, os, subprocess
SLOT_FILE = '$SLOT_FILE'
if not os.path.exists(SLOT_FILE):
    print('Chua co slot nao duoc tao.')
    exit(0)
with open(SLOT_FILE) as f:
    data = json.load(f)
if not data:
    print('Chua co slot nao duoc tao.')
for sid, item in data.items():
    pid_file = f'/opt/jar-manager/profiles/{item[\"acc\"]}/{item[\"acc\"]}.pid'
    status = 'STOPPED'
    if os.path.exists(pid_file):
        try:
            with open(pid_file) as pf: pid = pf.read().strip()
            res = subprocess.run(['ps', '-p', pid], stdout=subprocess.DEVNULL)
            if res.returncode == 0:
                status = f'RUNNING (PID {pid})'
        except: pass
    print(f\"{item['slot']:<6} | {item['game']:<16} | {item['acc']:<10} | {item['pin']:<6} | {item['port']:<8} | {status}\")
"
        echo "================================================================================"
        ;;

    stop)
        SLOT_ID="$2"
        if [ -z "$SLOT_ID" ]; then
            echo "Cach dung: $0 stop <slot_number>"
            exit 1
        fi
        ACC_NAME="slot${SLOT_ID}"
        echo "Dang dung game tren slot ${SLOT_ID} (${ACC_NAME})..."
        /opt/jar-manager/scripts/stop-acc.sh "$ACC_NAME" 2>/dev/null || true
        /usr/bin/vncserver -kill ":${SLOT_ID}" >/dev/null 2>&1 || true
        echo "Da dung slot ${SLOT_ID}."
        ;;

    *)
        echo "Cach dung: $0 {add|list|stop}"
        echo "  - $0 add <slot_id> <game_jar> <pin> [tieu_de]: Them 1 game moi cho khach moi"
        echo "  - $0 list: Xem danh sach cac game dang chay tren VPS"
        echo "  - $0 stop <slot_id>: Dung 1 slot game cu the"
        exit 1
        ;;
esac
