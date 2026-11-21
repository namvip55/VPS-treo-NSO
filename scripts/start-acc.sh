#!/bin/bash
ACC="${1:-acc1}"
JAR="${2:-/opt/jar-manager/games/game.jar}"

PROFILE_DIR="/opt/jar-manager/profiles/$ACC"
PID_FILE="$PROFILE_DIR/$ACC.pid"
mkdir -p "$PROFILE_DIR"

if [ -f "$PID_FILE" ]; then
    OLD_PID=$(cat "$PID_FILE")
    if ps -p "$OLD_PID" > /dev/null 2>&1; then
        echo "ACC_ALREADY_RUNNING:$OLD_PID"
        exit 0
    fi
fi

# Tự động thích ứng tài nguyên: Kiểm tra RAM khả dụng (Available RAM) và Tổng RAM
# Nếu VPS cấu hình 1:1 (RAM <= 1GB) hoặc RAM khả dụng <= 600MB (VPS đang gánh Web, Bot, API, Server...)
# thì tự động kích hoạt Ultra-Low Spec để chung sống hòa bình, không làm ảnh hưởng các dự án khác.
AVAILABLE_RAM_MB=$(free -m | awk '/^Mem:/{print $7}')
TOTAL_RAM_MB=$(free -m | awk '/^Mem:/{print $2}')

if [ "$TOTAL_RAM_MB" -le 1100 ] || [ "$AVAILABLE_RAM_MB" -le 600 ]; then
    # Cấu hình tối thiểu: Ép RAM cực nhẹ, triệt tiêu tải JIT
    JAVA_OPTS="-XX:+UseSerialGC -XX:TieredStopAtLevel=1 -Xms16m -Xmx32m"
else
    # Cấu hình tiêu chuẩn khi VPS còn nhiều RAM rảnh rỗi (mượt mà, không bị bóp)
    JAVA_OPTS="-XX:+UseSerialGC -Xms32m -Xmx64m"
fi

# Tối ưu hóa pipeline đồ họa Java2D trên màn hình ảo X11 (giảm tải CPU, tăng độ mượt)
JAVA_OPTS="$JAVA_OPTS -Dsun.java2d.pmoffscreen=false -Dsun.java2d.xrender=false"

# Tự động trích xuất MIDlet class và tên game từ MANIFEST.MF nếu có để vào thẳng game
MIDLET_CLASS=$(unzip -p "$JAR" META-INF/MANIFEST.MF 2>/dev/null | grep -i '^MIDlet-1:' | cut -d',' -f3 | tr -d ' \r\n')
MIDLET_NAME=$(unzip -p "$JAR" META-INF/MANIFEST.MF 2>/dev/null | grep -i '^MIDlet-Name:' | cut -d':' -f2 | tr -d '\r\n' | sed 's/^ *//')
WINDOW_TITLE="${MIDLET_NAME:-$(basename "$JAR" .jar)}"

export DISPLAY=:1
if [ -n "$MIDLET_CLASS" ]; then
    setsid java $JAVA_OPTS -Duser.home="$PROFILE_DIR" -jar /opt/jar-manager/emulator/microemulator.jar --resizableDevice 800 550 "$MIDLET_CLASS" "$JAR" > /dev/null 2>&1 &
else
    setsid java $JAVA_OPTS -Duser.home="$PROFILE_DIR" -jar /opt/jar-manager/emulator/microemulator.jar --resizableDevice 800 550 "$JAR" > /dev/null 2>&1 &
fi

NEW_PID=$!
echo "$NEW_PID" > "$PID_FILE"
echo "STARTED:$NEW_PID"

(
    sleep 2
    WIDS=$(xdotool search --pid "$NEW_PID" 2>/dev/null)
    for W in $WIDS; do
        xdotool set_window --name "$WINDOW_TITLE" "$W" 2>/dev/null
        wmctrl -ir "$W" -b add,maximized_vert,maximized_horz 2>/dev/null
        xdotool windowactivate "$W" 2>/dev/null
        sleep 0.5
        xdotool key --window "$W" Return 2>/dev/null
    done
) &
