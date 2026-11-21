#!/bin/bash
# ==============================================================================
# CLIENT RESTRICTED COMMAND WRAPPER
# Giới hạn tuyệt đối quyền hạn của khóa SSH cấp cho khách hàng.
# Khách chỉ được dùng SSH Tunnel (cổng 5901) và gọi các script quản lý game.
# Cấm hoàn toàn shell tương tác và lệnh ngoài whitelist.
# ==============================================================================

# 1. Từ chối phiên shell tương tác không có lệnh
if [ -z "$SSH_ORIGINAL_COMMAND" ]; then
    echo "Access Denied: Interactive shell is not permitted for client role." >&2
    exit 1
fi

# 2. Phân rã lệnh và kiểm tra an toàn đối số
read -r CMD ARG1 ARG2 <<< "$SSH_ORIGINAL_COMMAND"

# Từ chối nếu có tham số thừa ngoài mong đợi (ARG2)
if [ -n "$ARG2" ]; then
    echo "Access Denied: Excess arguments are not permitted." >&2
    exit 1
fi

# Kiểm tra định dạng tên account (chỉ cho phép a-z, A-Z, 0-9, gạch dưới, gạch ngang)
if [ -n "$ARG1" ] && ! [[ "$ARG1" =~ ^[a-zA-Z0-9_-]+$ ]]; then
    echo "Access Denied: Invalid account parameter." >&2
    exit 1
fi

# 3. Whitelist các lệnh được phép thực thi
case "$CMD" in
    "/opt/jar-manager/scripts/mode-active.sh")
        exec /opt/jar-manager/scripts/mode-active.sh "$ARG1"
        ;;
    "/opt/jar-manager/scripts/mode-idle.sh")
        exec /opt/jar-manager/scripts/mode-idle.sh "$ARG1"
        ;;
    "/opt/jar-manager/scripts/start-acc.sh")
        exec /opt/jar-manager/scripts/start-acc.sh "$ARG1"
        ;;
    "/opt/jar-manager/scripts/stop-acc.sh")
        exec /opt/jar-manager/scripts/stop-acc.sh "$ARG1"
        ;;
    "/opt/jar-manager/scripts/status-all.sh")
        exec /opt/jar-manager/scripts/status-all.sh
        ;;
    *)
        echo "Access Denied: Command is not permitted for client role." >&2
        exit 1
        ;;
esac
