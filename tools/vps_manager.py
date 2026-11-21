#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
MULTI-VPS MANAGER - QUẢN LÝ & CẬP NHẬT TỪNG VPS RIÊNG BIỆT
Cho phép bạn quản lý danh sách nhiều VPS (của bạn hoặc khách hàng),
kiểm tra sức khỏe, cập nhật game (.jar) hoặc script riêng cho từng VPS mà không ảnh hưởng lẫn nhau.
"""

import os
import sys
import json
import argparse
import subprocess

BASE_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REGISTRY_FILE = os.path.join(BASE_DIR, "vps_registry.json")

def load_registry():
    if not os.path.exists(REGISTRY_FILE):
        return {}
    with open(REGISTRY_FILE, "r", encoding="utf-8") as f:
        return json.load(f)

def save_registry(data):
    with open(REGISTRY_FILE, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, ensure_ascii=False)

def get_ssh_cmd(vps_cfg, remote_cmd):
    key = os.path.expanduser(vps_cfg.get("key", "~/.ssh/id_ed25519_jar"))
    host = vps_cfg["host"]
    port = vps_cfg["port"]
    user = vps_cfg.get("user", "root")
    
    if os.path.exists(key):
        return f'ssh -i "{key}" -p {port} -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null {user}@{host} "{remote_cmd}"'
    else:
        return f'ssh -p {port} -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null {user}@{host} "{remote_cmd}"'

def run_ssh(vps_cfg, remote_cmd, capture=True):
    cmd = get_ssh_cmd(vps_cfg, remote_cmd)
    res = subprocess.run(cmd, shell=True, capture_output=capture, text=True)
    return res

def cmd_list(args):
    reg = load_registry()
    print("\n" + "=" * 75)
    print(f"{'ID':<10} | {'TÊN VPS':<25} | {'HOST:PORT':<22} | {'GAME ĐANG CHẠY'}")
    print("-" * 75)
    for vps_id, cfg in reg.items():
        hp = f"{cfg.get('host')}:{cfg.get('port')}"
        print(f"{vps_id:<10} | {cfg.get('name', 'N/A'):<25} | {hp:<22} | {cfg.get('game', 'game.jar')}")
    print("=" * 75 + "\n")

def cmd_status(args):
    reg = load_registry()
    targets = []
    if args.all:
        targets = list(reg.items())
    elif args.vps:
        if args.vps not in reg:
            print(f"[!] Khong tim thay VPS '{args.vps}' trong vps_registry.json")
            return
        targets = [(args.vps, reg[args.vps])]
    else:
        print("[!] Vui long chi dinh --vps <id> hoac --all de kiem tra toan bo.")
        return

    print("\n" + "=" * 80)
    print(f"{'ID':<8} | {'HOST:PORT':<20} | {'TRẠNG THÁI':<12} | {'RAM AVAILABLE':<15} | {'GAME ACC'}")
    print("-" * 80)

    for vps_id, cfg in targets:
        hp = f"{cfg.get('host')}:{cfg.get('port')}"
        res = run_ssh(cfg, "free -m | awk '/^Mem:/{print $7}'; /opt/jar-manager/scripts/status-all.sh 2>/dev/null")
        if res.returncode == 0:
            lines = res.stdout.strip().splitlines()
            avail_ram = f"{lines[0]} MB" if len(lines) > 0 and lines[0].isdigit() else "N/A"
            accs = [l.strip() for l in lines[1:] if "ACC:" in l]
            acc_str = ", ".join(accs) if accs else "Khong co acc"
            print(f"{vps_id:<8} | {hp:<20} | {'ONLINE':<12} | {avail_ram:<15} | {acc_str}")
        else:
            print(f"{vps_id:<8} | {hp:<20} | {'OFFLINE':<12} | {'-':<15} | Mat ket noi")
    print("=" * 80 + "\n")

def cmd_update_game(args):
    reg = load_registry()
    if args.vps not in reg:
        print(f"[!] Khong tim thay VPS '{args.vps}' trong vps_registry.json")
        return
    
    cfg = reg[args.vps]
    jar_path = args.jar
    if not os.path.exists(jar_path):
        print(f"[!] Khong tim thay file JAR: {jar_path}")
        return

    jar_name = os.path.basename(jar_path)
    print(f"\n[*] Dang cap nhat game [{jar_name}] cho [{args.vps} - {cfg.get('name')}]...")

    key = os.path.expanduser(cfg.get("key", "~/.ssh/id_ed25519_jar"))
    host = cfg["host"]
    port = cfg["port"]
    user = cfg.get("user", "root")

    # 1. Đẩy file JAR lên VPS
    if os.path.exists(key):
        scp_cmd = f'scp -i "{key}" -P {port} -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null "{jar_path}" {user}@{host}:/opt/jar-manager/games/{jar_name}'
    else:
        scp_cmd = f'scp -P {port} -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null "{jar_path}" {user}@{host}:/opt/jar-manager/games/{jar_name}'
    
    print(f">> Uploading {jar_name} -> {host}:{port}...")
    subprocess.run(scp_cmd, shell=True, check=True)

    # 2. Cập nhật symlink game.jar
    print(">> Updating symlink game.jar...")
    run_ssh(cfg, f"ln -sf /opt/jar-manager/games/{jar_name} /opt/jar-manager/games/game.jar", capture=False)

    # 3. Khởi động lại acc nếu có yêu cầu
    if args.restart_acc:
        acc = args.restart_acc
        print(f">> Restarting account [{acc}] tren {args.vps}...")
        run_ssh(cfg, f"/opt/jar-manager/scripts/stop-acc.sh {acc} && sleep 1 && /opt/jar-manager/scripts/start-acc.sh {acc}", capture=False)

    # 4. Cập nhật registry
    cfg["game"] = jar_name
    save_registry(reg)
    print(f"[+] CAP NHAT THANH CONG CHO VPS [{args.vps}]!\n")

def cmd_update_scripts(args):
    reg = load_registry()
    if args.vps not in reg:
        print(f"[!] Khong tim thay VPS '{args.vps}' trong vps_registry.json")
        return
    cfg = reg[args.vps]
    print(f"\n[*] Dong bo tap lenh scripts -> VPS [{args.vps}] ({cfg['host']}:{cfg['port']})...")

    key = os.path.expanduser(cfg.get("key", "~/.ssh/id_ed25519_jar"))
    host = cfg["host"]
    port = cfg["port"]
    user = cfg.get("user", "root")
    local_scripts = os.path.join(BASE_DIR, "scripts", "*")

    if os.path.exists(key):
        scp_cmd = f'scp -i "{key}" -P {port} -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null {local_scripts} {user}@{host}:/opt/jar-manager/scripts/'
    else:
        scp_cmd = f'scp -P {port} -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null {local_scripts} {user}@{host}:/opt/jar-manager/scripts/'

    subprocess.run(scp_cmd, shell=True, check=True)
    run_ssh(cfg, "sed -i 's/\\r$//' /opt/jar-manager/scripts/*.sh && chmod +x /opt/jar-manager/scripts/*.sh", capture=False)
    print(f"[+] Dong bo scripts thanh cong cho VPS [{args.vps}]!\n")

def cmd_export_client(args):
    reg = load_registry()
    if args.vps not in reg:
        print(f"[!] Khong tim thay VPS '{args.vps}' trong vps_registry.json")
        return
    cfg = reg[args.vps]
    package_script = os.path.join(BASE_DIR, "tools", "package_client.py")
    title = cfg.get("name", "NSO Client")
    key = cfg.get("key", "")
    mode = args.mode or "exe"

    cmd = [
        sys.executable,
        package_script,
        "--host", cfg["host"],
        "--port", str(cfg["port"]),
        "--title", title,
        "--mode", mode
    ]
    if key:
        cmd.extend(["--key", os.path.expanduser(key)])
    subprocess.run(cmd)

def main():
    parser = argparse.ArgumentParser(description="Multi-VPS Manager - Quản lý và cập nhật từng VPS riêng biệt")
    subparsers = parser.add_subparsers(dest="command", help="Lệnh thực hiện")

    # list
    p_list = subparsers.add_parser("list", help="Xem danh sách tất cả VPS đã đăng ký")
    p_list.set_defaults(func=cmd_list)

    # status
    p_status = subparsers.add_parser("status", help="Kiểm tra trạng thái VPS")
    p_status.add_argument("--vps", help="ID của VPS cần kiểm tra")
    p_status.add_argument("--all", action="store_true", help="Kiểm tra toàn bộ danh sách VPS")
    p_status.set_defaults(func=cmd_status)

    # update-game
    p_up_game = subparsers.add_parser("update-game", help="Cập nhật bản game .JAR cho đúng 1 VPS chỉ định")
    p_up_game.add_argument("--vps", required=True, help="ID của VPS cần cập nhật")
    p_up_game.add_argument("--jar", required=True, help="Đường dẫn đến file JAR mới")
    p_up_game.add_argument("--restart-acc", default="acc1", help="Tự động khởi động lại acc sau khi nạp (mặc định: acc1)")
    p_up_game.set_defaults(func=cmd_update_game)

    # update-scripts
    p_up_scripts = subparsers.add_parser("update-scripts", help="Đồng bộ các script quản lý mới nhất cho 1 VPS")
    p_up_scripts.add_argument("--vps", required=True, help="ID của VPS cần cập nhật")
    p_up_scripts.set_defaults(func=cmd_update_scripts)

    # export-client
    p_export = subparsers.add_parser("export-client", help="Đóng gói bộ cài EXE/ZIP giao cho khách của VPS đó")
    p_export.add_argument("--vps", required=True, help="ID của VPS")
    p_export.add_argument("--mode", choices=["exe", "zip"], default="exe", help="Định dạng xuất (exe hoặc zip)")
    p_export.set_defaults(func=cmd_export_client)

    args = parser.parse_args()
    if not hasattr(args, "func"):
        parser.print_help()
        return
    args.func(args)

if __name__ == "__main__":
    main()
