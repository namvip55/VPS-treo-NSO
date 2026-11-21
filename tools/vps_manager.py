#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
MULTI-VPS MANAGER - QUẢN LÝ & CẬP NHẬT TỪNG VPS RIÊNG BIỆT (CHỐNG NHẦM GAME)
Cấu trúc phân vùng độc lập: Mỗi VPS có thư mục riêng trong servers/<vps_id>/
Agent và User có thể cập nhật, kiểm tra hoặc xuất client cho từng VPS một cách tuyệt đối an toàn.
"""

import os
import sys
import json
import argparse
import subprocess
import shutil

BASE_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REGISTRY_FILE = os.path.join(BASE_DIR, "vps_registry.json")
SERVERS_DIR = os.path.join(BASE_DIR, "servers")

def load_registry():
    if not os.path.exists(REGISTRY_FILE):
        return {"active_vps": "vps1", "servers": {}}
    with open(REGISTRY_FILE, "r", encoding="utf-8") as f:
        data = json.load(f)
        if "servers" not in data:
            data = {"active_vps": list(data.keys())[0] if data else "vps1", "servers": data}
        return data

def save_registry(data):
    with open(REGISTRY_FILE, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, ensure_ascii=False)

def get_servers():
    reg = load_registry()
    return reg.get("servers", {})

def get_ssh_cmd(vps_cfg, remote_cmd):
    key = os.path.expanduser(vps_cfg.get("key", "~/.ssh/id_ed25519_jar"))
    host = vps_cfg["host"]
    port = vps_cfg["port"]
    user = vps_cfg.get("user", "root")
    
    key_opt = f'-i "{key}" ' if os.path.exists(key) else ''
    return f'ssh {key_opt}-p {port} -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o ConnectTimeout=8 {user}@{host} "{remote_cmd}"'

def run_ssh(vps_cfg, remote_cmd, capture=True):
    cmd = get_ssh_cmd(vps_cfg, remote_cmd)
    res = subprocess.run(cmd, shell=True, capture_output=capture, text=True)
    return res

def cmd_list(args):
    servers = get_servers()
    reg = load_registry()
    active = reg.get("active_vps", "")
    print("\n" + "=" * 85)
    print(f"{'ID':<12} | {'TÊN VPS':<24} | {'HOST:PORT':<20} | {'GAME FILE':<15} | {'ACTIVE'}")
    print("-" * 85)
    for vps_id, cfg in servers.items():
        hp = f"{cfg.get('host')}:{cfg.get('port')}"
        is_act = " [*] ACTIVE" if vps_id == active else ""
        print(f"{vps_id:<12} | {cfg.get('name', 'N/A'):<24} | {hp:<20} | {cfg.get('game', 'game.jar'):<15} | {is_act}")
    print("=" * 85 + "\n")

def cmd_status(args):
    servers = get_servers()
    targets = []
    if args.all:
        targets = list(servers.items())
    elif args.vps:
        if args.vps not in servers:
            print(f"[!] Khong tim thay VPS '{args.vps}' trong danh ba.")
            return
        targets = [(args.vps, servers[args.vps])]
    else:
        # Default to active_vps
        reg = load_registry()
        act = reg.get("active_vps")
        if act and act in servers:
            targets = [(act, servers[act])]
        else:
            print("[!] Vui long chi dinh --vps <id> hoac --all de kiem tra.")
            return

    print("\n" + "=" * 80)
    print(f"{'ID':<10} | {'HOST:PORT':<20} | {'TRẠNG THÁI':<12} | {'RAM AVAILABLE':<15} | {'GAME ACC'}")
    print("-" * 80)

    for vps_id, cfg in targets:
        hp = f"{cfg.get('host')}:{cfg.get('port')}"
        res = run_ssh(cfg, "free -m | awk '/^Mem:/{print $7}'; /opt/jar-manager/scripts/status-all.sh 2>/dev/null")
        if res.returncode == 0:
            lines = res.stdout.strip().splitlines()
            avail_ram = f"{lines[0]} MB" if len(lines) > 0 and lines[0].isdigit() else "N/A"
            accs = [l.strip() for l in lines[1:] if "ACC:" in l]
            acc_str = ", ".join(accs) if accs else "Khong co acc"
            print(f"{vps_id:<10} | {hp:<20} | {'ONLINE':<12} | {avail_ram:<15} | {acc_str}")
        else:
            print(f"{vps_id:<10} | {hp:<20} | {'OFFLINE':<12} | {'-':<15} | Mat ket noi")
    print("=" * 80 + "\n")

def cmd_update_game(args):
    servers = get_servers()
    vps_id = args.vps
    if vps_id not in servers:
        print(f"[!] Khong tim thay VPS '{vps_id}' trong registry.")
        return
    
    cfg = servers[vps_id]
    vps_dir = os.path.join(SERVERS_DIR, vps_id)
    os.makedirs(vps_dir, exist_ok=True)

    # Xác định file JAR nguồn:
    # Nếu truyền --jar thì copy file đó vào servers/<vps_id>/
    # Nếu không truyền --jar thì lấy file game mặc định từ servers/<vps_id>/
    jar_path = args.jar
    if jar_path:
        if not os.path.exists(jar_path):
            print(f"[!] File JAR khong ton tai: {jar_path}")
            return
        dest_local_jar = os.path.join(vps_dir, os.path.basename(jar_path))
        if os.path.abspath(jar_path) != os.path.abspath(dest_local_jar):
            shutil.copy2(jar_path, dest_local_jar)
        jar_path = dest_local_jar
    else:
        # Lấy file game từ thư mục servers/<vps_id>/
        game_name = cfg.get("game", "game.jar")
        jar_path = os.path.join(vps_dir, game_name)
        if not os.path.exists(jar_path):
            # Fallback nếu ở root
            root_fallback = os.path.join(BASE_DIR, game_name)
            if os.path.exists(root_fallback):
                shutil.copy2(root_fallback, jar_path)
            else:
                print(f"[!] Khong tim thay file game cho '{vps_id}' tai: {jar_path}")
                print(f"    Vui long truyen tham so: --jar <duong_dan_file_jar>")
                return

    jar_name = os.path.basename(jar_path)
    print(f"\n[*] ĐANG CẬP NHẬT GAME CHO [{vps_id} - {cfg.get('name')}]")
    print(f"    File JAR: {jar_path}")
    print(f"    Dích đến: {cfg['host']}:{cfg['port']}:/opt/jar-manager/games/{jar_name}")

    key = os.path.expanduser(cfg.get("key", "~/.ssh/id_ed25519_jar"))
    host = cfg["host"]
    port = cfg["port"]
    user = cfg.get("user", "root")
    key_opt = f'-i "{key}" ' if os.path.exists(key) else ''
    scp_cmd = f'scp {key_opt}-P {port} -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o ConnectTimeout=8 "{jar_path}" {user}@{host}:/opt/jar-manager/games/{jar_name}'
    
    print(f">> 1. Uploading file game sang VPS...")
    subprocess.run(scp_cmd, shell=True, check=True)

    print(f">> 2. Cap nhat symlink game.jar tren VPS...")
    run_ssh(cfg, f"ln -sf /opt/jar-manager/games/{jar_name} /opt/jar-manager/games/game.jar", capture=False)

    if args.restart_acc:
        acc = args.restart_acc
        print(f">> 3. Restarting account [{acc}] tren VPS...")
        run_ssh(cfg, f"/opt/jar-manager/scripts/stop-acc.sh {acc} && sleep 1 && /opt/jar-manager/scripts/start-acc.sh {acc}", capture=False)

    # Cập nhật thông tin vào registry & file config riêng
    cfg["game"] = jar_name
    reg = load_registry()
    reg["servers"][vps_id] = cfg
    save_registry(reg)

    # Lưu config.json riêng trong servers/<vps_id>/
    local_cfg_path = os.path.join(vps_dir, "config.json")
    with open(local_cfg_path, "w", encoding="utf-8") as f:
        json.dump(cfg, f, indent=2, ensure_ascii=False)

    print(f"\n[+] CẬP NHẬT HOÀN TẤT CHO VPS [{vps_id}]! CÁC VPS KHÁC HOÀN TOÀN KHÔNG BỊ ẢNH HƯỞNG.\n")

def cmd_update_scripts(args):
    servers = get_servers()
    vps_id = args.vps
    if vps_id not in servers:
        print(f"[!] Khong tim thay VPS '{vps_id}' trong registry.")
        return
    cfg = servers[vps_id]
    print(f"\n[*] Dong bo tap lenh scripts -> VPS [{vps_id}] ({cfg['host']}:{cfg['port']})...")

    key = os.path.expanduser(cfg.get("key", "~/.ssh/id_ed25519_jar"))
    host = cfg["host"]
    port = cfg["port"]
    user = cfg.get("user", "root")
    
    scripts_dir = os.path.join(BASE_DIR, "scripts")
    script_files = [os.path.join(scripts_dir, f) for f in os.listdir(scripts_dir) if f.endswith(".sh")]
    quoted_files = " ".join([f'"{f}"' for f in script_files])

    key_opt = f'-i "{key}" ' if os.path.exists(key) else ''
    scp_cmd = f'scp {key_opt}-P {port} -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o ConnectTimeout=8 {quoted_files} {user}@{host}:/opt/jar-manager/scripts/'

    subprocess.run(scp_cmd, shell=True, check=True)
    run_ssh(cfg, "sed -i 's/\\r$//' /opt/jar-manager/scripts/*.sh && chmod +x /opt/jar-manager/scripts/*.sh", capture=False)
    print(f"[+] Dong bo scripts thanh cong cho VPS [{vps_id}]!\n")

def cmd_export_client(args):
    servers = get_servers()
    vps_id = args.vps
    if vps_id not in servers:
        print(f"[!] Khong tim thay VPS '{vps_id}' trong registry.")
        return
    cfg = servers[vps_id]
    package_script = os.path.join(BASE_DIR, "tools", "package_client.py")
    title = cfg.get("game_title", cfg.get("name", "NSO Client"))
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
    p_up_game.add_argument("--vps", required=True, help="ID của VPS cần cập nhật (ví dụ: vps1)")
    p_up_game.add_argument("--jar", help="Đường dẫn đến file JAR mới (nếu bỏ trống sẽ lấy file từ servers/<vps_id>/)")
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
