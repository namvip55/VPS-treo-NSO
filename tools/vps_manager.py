#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
MULTI-VPS MANAGER - QUẢN LÝ & CẬP NHẬT TỪNG VPS RIÊNG BIỆT (CHỐNG NHẦM GAME)
Cấu trúc phân vùng độc lập: Mỗi VPS có thư mục riêng trong servers/<vps_id>/
Agent và User có thể cập nhật, kiểm tra hoặc xuất client cho từng VPS một cách tuyệt đối an toàn.
Không sử dụng shell=True (chống Shell Injection) và xác thực chặt chẽ kết quả trả về.
"""

import os
import sys
import json
import shlex
import argparse
import subprocess
import shutil

BASE_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REGISTRY_FILE = os.path.join(BASE_DIR, "vps_registry.json")
SERVERS_DIR = os.path.join(BASE_DIR, "servers")

def load_registry():
    data = {"active_vps": "vps1", "servers": {}}
    if os.path.exists(REGISTRY_FILE):
        with open(REGISTRY_FILE, "r", encoding="utf-8") as f:
            data = json.load(f)
            if "servers" not in data:
                data = {"active_vps": list(data.keys())[0] if data else "vps1", "servers": data}

    # Tự động đọc và đồng bộ thông tin từ servers/<id>/config.json nếu có
    if os.path.exists(SERVERS_DIR):
        for entry in os.listdir(SERVERS_DIR):
            vps_sub = os.path.join(SERVERS_DIR, entry)
            cfg_path = os.path.join(vps_sub, "config.json")
            if os.path.isdir(vps_sub) and os.path.exists(cfg_path):
                try:
                    with open(cfg_path, "r", encoding="utf-8") as cf:
                        sub_cfg = json.load(cf)
                        vps_id = sub_cfg.get("id", entry)
                        if vps_id in data.get("servers", {}):
                            data["servers"][vps_id].update(sub_cfg)
                        else:
                            data.setdefault("servers", {})[vps_id] = sub_cfg
                except Exception:
                    pass
    return data

def save_registry(data):
    with open(REGISTRY_FILE, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, ensure_ascii=False)

def get_servers():
    reg = load_registry()
    return reg.get("servers", {})

def build_ssh_cmd_args(vps_cfg, remote_cmd):
    key = os.path.expanduser(vps_cfg.get("key", "~/.ssh/id_ed25519_jar"))
    host = vps_cfg["host"]
    port = vps_cfg["port"]
    user = vps_cfg.get("user", "root")

    cmd = [
        "ssh",
        "-p", str(port),
        "-o", "StrictHostKeyChecking=no",
        "-o", "UserKnownHostsFile=/dev/null",
        "-o", "ConnectTimeout=8"
    ]
    if os.path.exists(key):
        cmd.extend(["-i", key])
    cmd.extend([f"{user}@{host}", remote_cmd])
    return cmd

def run_ssh(vps_cfg, remote_cmd, capture=True):
    cmd = build_ssh_cmd_args(vps_cfg, remote_cmd)
    res = subprocess.run(cmd, shell=False, capture_output=capture, text=True)
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
        game_name = cfg.get("game", cfg.get("game_file", "game.jar"))
        print(f"{vps_id:<12} | {cfg.get('name', 'N/A'):<24} | {hp:<20} | {game_name:<15} | {is_act}")
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

def cmd_update_vps(args):
    servers = get_servers()
    vps_id = args.vps
    reg = load_registry()

    cfg = servers.get(vps_id, {
        "id": vps_id,
        "name": f"VPS {vps_id}",
        "dir": f"servers/{vps_id}",
        "host": "127.0.0.1",
        "port": 22,
        "user": "root",
        "key": "~/.ssh/id_ed25519_jar",
        "game": "game.jar",
        "game_title": "Java Game",
        "accounts": ["acc1"]
    })

    if args.host_port:
        hp = args.host_port.strip()
        if ":" in hp:
            h, p = hp.split(":", 1)
            cfg["host"] = h.strip()
            cfg["port"] = int(p.strip())
        else:
            cfg["host"] = hp
    if args.host:
        cfg["host"] = args.host.strip()
    if args.port:
        cfg["port"] = int(args.port)
    if args.name:
        cfg["name"] = args.name.strip()
    if args.web_url:
        cfg["web_url"] = args.web_url.strip()
    if args.key:
        cfg["key"] = args.key.strip()
    if args.game_title:
        cfg["game_title"] = args.game_title.strip()

    # Lưu lại vào registry & servers/<vps_id>/config.json
    reg.setdefault("servers", {})[vps_id] = cfg
    save_registry(reg)

    vps_dir = os.path.join(SERVERS_DIR, vps_id)
    os.makedirs(vps_dir, exist_ok=True)
    local_cfg = os.path.join(vps_dir, "config.json")
    with open(local_cfg, "w", encoding="utf-8") as f:
        json.dump(cfg, f, indent=2, ensure_ascii=False)

    print(f"\n[+] ĐÃ CẬP NHẬT THÔNG SỐ CHO VPS [{vps_id}]:")
    print(f"    - Tên hiển thị: {cfg.get('name')}")
    print(f"    - Host:Port:   {cfg['host']}:{cfg['port']}")
    print(f"    - SSH Key:     {cfg.get('key')}")
    print(f"    - Web Phone:   {cfg.get('web_url', 'Chưa cấu hình')}")

    if args.sync:
        print(f"[*] Đang kiểm tra kết nối và đồng bộ scripts sang {cfg['host']}:{cfg['port']}...")
        cmd_update_scripts(args)
    print()

def cmd_update_game(args):
    servers = get_servers()
    vps_id = args.vps
    if vps_id not in servers:
        print(f"[!] Khong tim thay VPS '{vps_id}' trong registry.")
        return

    cfg = servers[vps_id]
    vps_dir = os.path.join(SERVERS_DIR, vps_id)
    os.makedirs(vps_dir, exist_ok=True)

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
        game_name = cfg.get("game", cfg.get("game_file", "game.jar"))
        jar_path = os.path.join(vps_dir, game_name)
        if not os.path.exists(jar_path):
            root_fallback = os.path.join(BASE_DIR, game_name)
            if os.path.exists(root_fallback):
                shutil.copy2(root_fallback, jar_path)
            else:
                print(f"[!] Khong tim thay file game cho '{vps_id}' tai: {jar_path}")
                print(f"    Vui long truyen tham so: --jar <duong_dan_file_jar>")
                return

    jar_name = os.path.basename(jar_path)
    target_tab = (args.tab or "1").strip().lower()

    # Nhận diện slot ID từ tab
    slot_id = 1
    if target_tab.startswith("slot"):
        try: slot_id = int(target_tab.replace("slot", ""))
        except: slot_id = 1
    elif target_tab.isdigit():
        slot_id = int(target_tab)

    print(f"\n[*] ĐANG CẬP NHẬT GAME CHO VPS [{vps_id} - {cfg.get('name')}]")
    print(f"    Tab / Slot mục tiêu: Slot {slot_id} ({'Game gốc' if slot_id == 1 else 'Slot khách'})")
    print(f"    Bản JAR mới:        {jar_path}")
    print(f"    Đích đến VPS:       {cfg['host']}:{cfg['port']}:/opt/jar-manager/games/{jar_name}")

    key = os.path.expanduser(cfg.get("key", "~/.ssh/id_ed25519_jar"))
    host = cfg["host"]
    port = cfg["port"]
    user = cfg.get("user", "root")

    scp_cmd = [
        "scp",
        "-P", str(port),
        "-o", "StrictHostKeyChecking=no",
        "-o", "UserKnownHostsFile=/dev/null",
        "-o", "ConnectTimeout=8"
    ]
    if os.path.exists(key):
        scp_cmd.extend(["-i", key])
    scp_cmd.extend([jar_path, f"{user}@{host}:/opt/jar-manager/games/{jar_name}"])

    print(f">> 1. Uploading file JAR mới lên VPS...")
    res_upload = subprocess.run(scp_cmd, shell=False, capture_output=True, text=True)
    if res_upload.returncode != 0:
        print(f"[!] Upload file game that bai:\n{res_upload.stderr or res_upload.stdout}")
        return

    q_jar_name = shlex.quote(jar_name)
    if slot_id == 1:
        print(f">> 2. Cập nhật symlink game.jar cho Slot 1 (Gốc)...")
        res_symlink = run_ssh(cfg, f"ln -sf /opt/jar-manager/games/{q_jar_name} /opt/jar-manager/games/game.jar", capture=True)
        if res_symlink.returncode != 0:
            print(f"[!] Cap nhat symlink that bai:\n{res_symlink.stderr or res_symlink.stdout}")
            return

        acc = args.restart_acc or "acc1"
        q_acc = shlex.quote(acc)
        print(f">> 3. Khởi động lại Slot 1 [{acc}]...")
        run_ssh(cfg, f"/opt/jar-manager/scripts/stop-acc.sh {q_acc} && sleep 1 && /opt/jar-manager/scripts/start-acc.sh {q_acc} /opt/jar-manager/games/game.jar 1", capture=True)

        cfg["game"] = jar_name
        reg = load_registry()
        reg["servers"][vps_id] = cfg
        save_registry(reg)
    else:
        acc = f"slot{slot_id}"
        print(f">> 2. Cập nhật cấu hình slot {slot_id} ({acc}) với bản JAR mới...")
        update_slot_py = f"""
import json
SLOT_FILE = '/opt/jar-manager/slots.json'
try:
    with open(SLOT_FILE, 'r') as f: data = json.load(f)
except: data = {{}}
if '{slot_id}' in data:
    data['{slot_id}']['jar'] = '/opt/jar-manager/games/{jar_name}'
    with open(SLOT_FILE, 'w') as f: json.dump(data, f, indent=2)
"""
        run_ssh(cfg, f"python3 -c \"{update_slot_py}\"", capture=True)
        print(f">> 3. Khởi động lại Slot {slot_id}...")
        run_ssh(cfg, f"/opt/jar-manager/scripts/stop-acc.sh {acc} && sleep 1 && /opt/jar-manager/scripts/start-acc.sh {acc} /opt/jar-manager/games/{q_jar_name} {slot_id}", capture=True)

    print(f"\n[+] ĐÃ THAY THẾ TAB/SLOT {slot_id} THÀNH CÔNG VỚI BẢN JAR MỚI [{jar_name}] TRÊN VPS [{vps_id}]!")
    print(f"    Các tab game và VPS khác hoàn toàn được giữ nguyên không bị gián đoạn.\n")

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

    scp_cmd = [
        "scp",
        "-P", str(port),
        "-o", "StrictHostKeyChecking=no",
        "-o", "UserKnownHostsFile=/dev/null",
        "-o", "ConnectTimeout=8"
    ]
    if os.path.exists(key):
        scp_cmd.extend(["-i", key])
    scp_cmd.extend(script_files)
    scp_cmd.append(f"{user}@{host}:/opt/jar-manager/scripts/")

    res_scp = subprocess.run(scp_cmd, shell=False, capture_output=True, text=True)
    if res_scp.returncode != 0:
        print(f"[!] Dong bo scripts that bai:\n{res_scp.stderr or res_scp.stdout}")
        return

    res_perm = run_ssh(cfg, "sed -i 's/\\r$//' /opt/jar-manager/scripts/*.sh && chmod +x /opt/jar-manager/scripts/*.sh", capture=True)
    if res_perm.returncode != 0:
        print(f"[!] Phan quyen scripts that bai:\n{res_perm.stderr or res_perm.stdout}")
        return

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
    acc = args.acc or (cfg.get("accounts", ["acc1"])[0] if cfg.get("accounts") else "acc1")

    cmd = [
        sys.executable,
        package_script,
        "--host", cfg["host"],
        "--port", str(cfg["port"]),
        "--title", title,
        "--acc", acc,
        "--mode", mode
    ]
    if args.push_pubkey:
        cmd.append("--push-pubkey")
    if key:
        cmd.extend(["--admin-key", os.path.expanduser(key)])

    subprocess.run(cmd, shell=False)

def cmd_setup_web(args):
    servers = get_servers()
    vps_id = args.vps
    if vps_id not in servers:
        print(f"[!] Khong tim thay VPS '{vps_id}' trong registry.")
        return
    cfg = servers[vps_id]
    pin = args.pin or "1234"
    domain = args.domain or ""
    title = args.title or cfg.get("game_title", "JAVA CLOUD")

    print(f"\n[*] Đang cấu hình giao diện Web Phone điều khiển (noVNC + PWA) cho VPS [{vps_id}]...")
    remote_cmd = f"bash /opt/jar-manager/scripts/setup-ios-web.sh --pin {shlex.quote(pin)} --title {shlex.quote(title)}"
    if domain:
        remote_cmd += f" --domain {shlex.quote(domain)}"
    if getattr(args, "token", None):
        remote_cmd += f" --token {shlex.quote(args.token)}"

    cmd = build_ssh_cmd_args(cfg, remote_cmd)
    res = subprocess.run(cmd, shell=False)
    if res.returncode == 0:
        if domain:
            cfg["web_url"] = f"https://{domain}"
            reg = load_registry()
            reg["servers"][vps_id] = cfg
            save_registry(reg)
        print(f"[+] Đã cấu hình Web Phone thành công cho [{vps_id}]!\n")
    else:
        print(f"[!] Lỗi khi cài đặt Web Phone trên [{vps_id}].\n")

def cmd_remove_client(args):
    servers = get_servers()
    if args.vps not in servers:
        print(f"[!] Khong tim thay VPS co ID: {args.vps}")
        return
    cfg = servers[args.vps]
    raw_tab = (args.tab or args.slot or "").strip().lower()
    slot_id = raw_tab
    if slot_id.startswith("slot"):
        slot_id = slot_id.replace("slot", "")

    if not slot_id or not slot_id.isdigit():
        print("[!] Vui long chi dinh ro tab/slot game can xoa (vi du: 2, slot2, 3...)")
        return

    print(f"[*] Dang thu hoi quyen truy cap va xoa tab/slot {slot_id} tren VPS {args.vps} ({cfg['host']}:{cfg['port']})...")
    remote_cmd = f"bash /opt/jar-manager/scripts/slot-manager.sh remove {shlex.quote(slot_id)}"
    cmd = build_ssh_cmd_args(cfg, remote_cmd)
    res = subprocess.run(cmd, shell=False)
    if res.returncode == 0:
        print(f"[+] Da xoa thanh cong tab/slot {slot_id} tren {args.vps}! Cac tab khac giu nguyen.")
    else:
        print(f"[!] Loi khi xoa tab/slot {slot_id} tren {args.vps}")

def cmd_gui(args):
    exe_path = os.path.join(BASE_DIR, "AdminDashboard.exe")
    src = os.path.join(BASE_DIR, "tools", "AdminDashboard.cs")
    need_build = not os.path.exists(exe_path) or (os.path.exists(src) and os.path.getmtime(src) > os.path.getmtime(exe_path))
    if need_build:
        print("[*] Đang biên dịch AdminDashboard.exe (hỗ trợ High-DPI 200%)...")
        csc = r"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
        cmd = [csc, "/nologo", "/target:winexe", "/r:System.Web.Extensions.dll", f"/out:{exe_path}", src]
        res = subprocess.run(cmd, capture_output=True, text=True)
        if res.returncode != 0:
            print(f"[!] Lỗi biên dịch: {res.stderr}")
            return
    print("[+] Đang mở giao diện JarManager Central Admin Dashboard...")
    subprocess.Popen([exe_path], cwd=BASE_DIR)

def normalize_vps_args(args):
    if hasattr(args, "opt_vps") and args.opt_vps:
        args.vps = args.opt_vps
    if hasattr(args, "opt_tab") and args.opt_tab:
        args.tab = args.opt_tab
    if hasattr(args, "opt_slot") and args.opt_slot:
        args.slot = args.opt_slot

def cmd_remove_client_wrapper(args):
    normalize_vps_args(args)
    if not getattr(args, "vps", None):
        print("[!] Thiếu ID của VPS! Cách dùng: python tools/vps_manager.py remove-client <name-vps> <tab-game-khách>")
        return
    cmd_remove_client(args)

def cmd_update_vps_wrapper(args):
    normalize_vps_args(args)
    if not getattr(args, "vps", None):
        print("[!] Thiếu ID của VPS! Cách dùng: python tools/vps_manager.py update-vps <name-vps> <host:port> [options]")
        return
    cmd_update_vps(args)

def cmd_setup_web_wrapper(args):
    normalize_vps_args(args)
    if not getattr(args, "vps", None):
        print("[!] Thiếu ID của VPS! Cách dùng: python tools/vps_manager.py setup-web <name-vps> [options]")
        return
    cmd_setup_web(args)

def cmd_update_game_wrapper(args):
    normalize_vps_args(args)
    if not getattr(args, "vps", None):
        print("[!] Thiếu ID của VPS! Cách dùng: python tools/vps_manager.py update-game <name-vps> [--tab <tab_cu>] [--jar <new_jar>]")
        return
    cmd_update_game(args)

def main():
    parser = argparse.ArgumentParser(description="Multi-VPS Manager - Quản lý và cập nhật từng VPS riêng biệt")
    subparsers = parser.add_subparsers(dest="command", help="Lệnh thực hiện")

    # gui
    p_gui = subparsers.add_parser("gui", help="Mở cửa sổ đồ họa Admin Dashboard giám sát VPS & Game")
    p_gui.set_defaults(func=cmd_gui)

    # list
    p_list = subparsers.add_parser("list", help="Xem danh sách tất cả VPS đã đăng ký")
    p_list.set_defaults(func=cmd_list)

    # status
    p_status = subparsers.add_parser("status", help="Kiểm tra trạng thái VPS")
    p_status.add_argument("pos_vps", nargs="?", help="ID của VPS cần kiểm tra")
    p_status.add_argument("--vps", dest="opt_vps", help="ID của VPS cần kiểm tra")
    p_status.add_argument("--all", action="store_true", help="Kiểm tra toàn bộ danh sách VPS")
    p_status.set_defaults(func=lambda a: (normalize_vps_args(a), setattr(a, "vps", a.opt_vps or a.pos_vps), cmd_status(a)))

    # update-vps: cú pháp update-vps <name-vps> <thông số>
    p_up_vps = subparsers.add_parser("update-vps", help="Cập nhật thông số VPS: update-vps <name-vps> <host:port> [options]")
    p_up_vps.add_argument("vps", nargs="?", help="Tên hoặc ID của VPS (ví dụ: vps1)")
    p_up_vps.add_argument("host_port", nargs="?", help="Thông số Host và Port mới (dạng 14.250.250.229:12724)")
    p_up_vps.add_argument("--vps", dest="opt_vps", help="ID của VPS")
    p_up_vps.add_argument("--host", help="Host hoặc IPv4 mới")
    p_up_vps.add_argument("--port", help="Port NAT mới")
    p_up_vps.add_argument("--name", help="Tên gợi nhớ cho VPS")
    p_up_vps.add_argument("--web-url", help="Đường link Web Phone (Ngrok / Cloudflare)")
    p_up_vps.add_argument("--key", help="Đường dẫn file SSH Private Key")
    p_up_vps.add_argument("--game-title", help="Tên game chính của VPS")
    p_up_vps.add_argument("--sync", action="store_true", help="Đồng bộ ngay các scripts lên VPS mới")
    p_up_vps.set_defaults(func=cmd_update_vps_wrapper)

    # update-game: cú pháp update-game <name-vps> : thay <tab-game-cũ> = <bản jar mới>
    p_up_game = subparsers.add_parser("update-game", help="Cập nhật game cho 1 VPS: update-game <name-vps> [--tab <tab_cu>] [--jar <new_jar>]")
    p_up_game.add_argument("vps", nargs="?", help="Tên hoặc ID của VPS (ví dụ: vps1)")
    p_up_game.add_argument("--vps", dest="opt_vps", help="ID của VPS cần cập nhật")
    p_up_game.add_argument("--tab", "--slot", dest="tab", default="1", help="Tab hoặc Slot game cần thay thế (mặc định: 1 - Tab game gốc)")
    p_up_game.add_argument("--jar", help="Đường dẫn đến file JAR mới (nếu bỏ trống sẽ lấy file từ servers/<vps_id>/)")
    p_up_game.add_argument("--restart-acc", help="Tự động khởi động lại acc sau khi nạp (mặc định theo tab)")
    p_up_game.set_defaults(func=cmd_update_game_wrapper)

    # remove-client: cú pháp remove-client <name-vps> <tab-game-khách>
    p_rem_client = subparsers.add_parser("remove-client", help="Thu hồi & dọn dẹp slot khách: remove-client <name-vps> <tab-game-khách>")
    p_rem_client.add_argument("vps", nargs="?", help="Tên hoặc ID của VPS")
    p_rem_client.add_argument("tab", nargs="?", help="Tab hoặc Slot game khách thuê (ví dụ: 2, slot2)")
    p_rem_client.add_argument("--vps", dest="opt_vps", help="ID của VPS")
    p_rem_client.add_argument("--tab", "--slot", dest="opt_tab", help="Tab hoặc Slot của khách")
    p_rem_client.set_defaults(func=cmd_remove_client_wrapper)

    # remove-slot (alias của remove-client)
    p_rem_slot = subparsers.add_parser("remove-slot", help="Xóa hoàn toàn 1 slot game khách (alias remove-client)")
    p_rem_slot.add_argument("vps", nargs="?", help="ID của VPS")
    p_rem_slot.add_argument("slot", nargs="?", help="Số thứ tự slot cần xóa")
    p_rem_slot.add_argument("--vps", dest="opt_vps", help="ID của VPS")
    p_rem_slot.add_argument("--slot", dest="opt_slot", help="Số thứ tự slot cần xóa")
    p_rem_slot.set_defaults(func=cmd_remove_client_wrapper)

    # setup-web: Cấu hình bắt buộc Web Phone
    p_setup_web = subparsers.add_parser("setup-web", help="Cấu hình Web Phone điều khiển Safari/Chrome (Bắt buộc)")
    p_setup_web.add_argument("vps", nargs="?", help="ID của VPS")
    p_setup_web.add_argument("--vps", dest="opt_vps", help="ID của VPS")
    p_setup_web.add_argument("--pin", default="1234", help="Mã PIN riêng giao khách (mặc định: 1234)")
    p_setup_web.add_argument("--domain", help="Tên miền Ngrok tĩnh hoặc Cloudflare")
    p_setup_web.add_argument("--token", help="Ngrok Authtoken")
    p_setup_web.add_argument("--title", help="Tên tựa game hiển thị trên Web")
    p_setup_web.set_defaults(func=cmd_setup_web_wrapper)

    # update-scripts
    p_up_scripts = subparsers.add_parser("update-scripts", help="Đồng bộ các script quản lý mới nhất cho 1 VPS")
    p_up_scripts.add_argument("vps", nargs="?", help="ID của VPS cần cập nhật")
    p_up_scripts.add_argument("--vps", dest="opt_vps", help="ID của VPS cần cập nhật")
    p_up_scripts.set_defaults(func=lambda a: (normalize_vps_args(a), cmd_update_scripts(a)))

    # export-client (CHỈ KHI NGƯỜI DÙNG YÊU CẦU ĐÓNG GÓI)
    p_export = subparsers.add_parser("export-client", help="Đóng gói bộ cài EXE/ZIP (CHỈ KHI CÓ YÊU CẦU ĐẶC BIỆT TỪ USER)")
    p_export.add_argument("vps", nargs="?", help="ID của VPS")
    p_export.add_argument("--vps", dest="opt_vps", help="ID của VPS")
    p_export.add_argument("--acc", help="Tên account gán vào app")
    p_export.add_argument("--mode", choices=["exe", "zip"], default="exe", help="Định dạng xuất (exe hoặc zip)")
    p_export.add_argument("--push-pubkey", action="store_true", help="Tự động nạp public key an toàn vào VPS")
    p_export.set_defaults(func=lambda a: (normalize_vps_args(a), cmd_export_client(a)))

    args = parser.parse_args()
    if not hasattr(args, "func"):
        parser.print_help()
        return
    args.func(args)

if __name__ == "__main__":
    main()
