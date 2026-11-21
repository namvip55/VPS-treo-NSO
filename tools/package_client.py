#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
PACKAGE CLIENT TOOL - ĐÓNG GÓI BỘ ĐIỀU KHIỂN 1-CLICK CHO KHÁCH HÀNG
Tự động hóa 100% việc tạo file thực thi duy nhất (.exe) hoặc gói ZIP Portable
giao cho khách chơi game mà không cần khách phải cấu hình bất cứ thứ gì.
Đảm bảo an toàn bảo mật tuyệt đối: Không nhúng admin root key, giới hạn quyền qua wrapper.
"""

import os
import sys
import re
import argparse
import subprocess
import shutil
import zipfile
import tempfile

BASE_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
VNC_PATH = os.path.join(BASE_DIR, "vncviewer.exe")
TEMPLATE_CS = os.path.join(BASE_DIR, "tools", "ClientLauncher.cs")
CSC_EXE = r"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

def check_requirements():
    if not os.path.exists(CSC_EXE):
        print(f"[!] Khong tim thay trinh bien dich C# tai: {CSC_EXE}")
        sys.exit(1)
    if not os.path.exists(VNC_PATH):
        print(f"[*] Khong tim thay vncviewer.exe tai {VNC_PATH}, tien hanh tu dong tai ve...")
        try:
            from download_vnc import ensure_vncviewer
            if not ensure_vncviewer():
                sys.exit(1)
        except Exception:
            print(f"[!] Khong tim thay file vncviewer.exe tai: {VNC_PATH}")
            sys.exit(1)
    if not os.path.exists(TEMPLATE_CS):
        print(f"[!] Khong tim thay template C# tai: {TEMPLATE_CS}")
        sys.exit(1)

def generate_key_pair(output_prefix):
    """Sinh cap khoa SSH Ed25519 rieng biet cho khach hang"""
    priv_key = f"{output_prefix}.key"
    pub_key = f"{output_prefix}.key.pub"
    if os.path.exists(priv_key):
        os.remove(priv_key)
    if os.path.exists(pub_key):
        os.remove(pub_key)

    cmd = ["ssh-keygen", "-t", "ed25519", "-N", "", "-f", priv_key, "-C", "client-access"]
    subprocess.run(cmd, check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    return priv_key, pub_key

def push_client_pubkey_to_vps(host, port, pub_key_content, admin_key_path="~/.ssh/id_ed25519_jar"):
    """Nạp public key an toàn vào authorized_keys trên VPS với giới hạn tuyệt đối qua client-cmd-wrapper.sh"""
    admin_key = os.path.expanduser(admin_key_path)
    restricted_entry = (
        f'command="/opt/jar-manager/scripts/client-cmd-wrapper.sh",'
        f'permitopen="127.0.0.1:5901",'
        f'no-agent-forwarding,no-X11-forwarding,no-user-rc '
        f'{pub_key_content.strip()}'
    )

    remote_cmd = (
        f'mkdir -p /root/.ssh && chmod 700 /root/.ssh && '
        f'grep -qF "{pub_key_content.strip()}" /root/.ssh/authorized_keys 2>/dev/null || '
        f'echo \'{restricted_entry}\' >> /root/.ssh/authorized_keys && '
        f'chmod 600 /root/.ssh/authorized_keys'
    )

    cmd = [
        "ssh",
        "-p", str(port),
        "-o", "StrictHostKeyChecking=no",
        "-o", "UserKnownHostsFile=/dev/null",
        "-o", "ConnectTimeout=8"
    ]
    if os.path.exists(admin_key):
        cmd.extend(["-i", admin_key])
    cmd.extend([f"root@{host}", remote_cmd])

    print(f"[*] Dang day Public Key an toan sang VPS {host}:{port}...")
    res = subprocess.run(cmd, capture_output=True, text=True)
    if res.returncode != 0:
        print(f"[!] Khong the day public key len VPS:\n{res.stderr or res.stdout}")
        return False
    print("[+] Da nap thanh cong Public Key voi quyen gioi han (Tunnel VNC 5901 + client-cmd-wrapper) vao VPS!")
    return True

def patch_client_launcher_code(host, port, title, acc="acc1"):
    """Thay thế thông số cấu hình vào file template C# bằng regex an toàn"""
    with open(TEMPLATE_CS, "r", encoding="utf-8") as f:
        code = f.read()

    code, n_host = re.subn(r'public const string VPS_HOST\s*=\s*".*?";', f'public const string VPS_HOST = "{host}";', code)
    code, n_port = re.subn(r'public const int VPS_PORT\s*=\s*\d+;', f'public const int VPS_PORT = {port};', code)
    code, n_title = re.subn(r'public const string GAME_TITLE\s*=\s*".*?";', f'public const string GAME_TITLE = "{title}";', code)
    code, n_acc = re.subn(r'public const string TARGET_ACCOUNT\s*=\s*".*?";', f'public const string TARGET_ACCOUNT = "{acc}";', code)

    if n_host == 0 or n_port == 0 or n_title == 0 or n_acc == 0:
        raise ValueError(f"Không thể gán cấu hình vào ClientLauncher.cs! (host={n_host}, port={n_port}, title={n_title}, acc={n_acc})")
    return code

def build_standalone_exe(host, port, title, acc, key_path, out_exe_path):
    """Biên dịch file C# nhúng sẵn cấu hình, vncviewer và private key thành 1 file EXE duy nhất"""
    code = patch_client_launcher_code(host, port, title, acc)

    with tempfile.NamedTemporaryFile("w", suffix=".cs", encoding="utf-8", delete=False) as tmp_cs:
        tmp_cs.write(code)
        tmp_cs_path = tmp_cs.name

    try:
        cmd = [
            CSC_EXE,
            "/target:winexe",
            f"/out:{out_exe_path}",
            f"/resource:{VNC_PATH},VncViewer",
            f"/resource:{key_path},PrivateKey",
            tmp_cs_path
        ]
        res = subprocess.run(cmd, capture_output=True, text=True)
        if res.returncode != 0:
            print("[!] Loi bien dich C#:\n", res.stderr or res.stdout)
            sys.exit(1)
        print(f"[+] Da tao thanh cong file EXE doc lap: {out_exe_path} ({os.path.getsize(out_exe_path) / 1024 / 1024:.1f} MB)")
    finally:
        if os.path.exists(tmp_cs_path):
            os.remove(tmp_cs_path)

def build_zip_package(host, port, title, acc, key_path, out_zip_path):
    """Đóng gói dạng thư mục Portable ZIP gồm EXE + vncviewer + key"""
    temp_dir = tempfile.mkdtemp(prefix="client_pkg_")
    try:
        exe_path = os.path.join(temp_dir, f"{title}.exe")
        code = patch_client_launcher_code(host, port, title, acc)

        with tempfile.NamedTemporaryFile("w", suffix=".cs", encoding="utf-8", delete=False) as tmp_cs:
            tmp_cs.write(code)
            tmp_cs_path = tmp_cs.name

        try:
            cmd = [
                CSC_EXE,
                "/target:winexe",
                f"/out:{exe_path}",
                tmp_cs_path
            ]
            res = subprocess.run(cmd, capture_output=True, text=True)
            if res.returncode != 0:
                print("[!] Loi bien dich C#:\n", res.stderr or res.stdout)
                sys.exit(1)
        finally:
            if os.path.exists(tmp_cs_path):
                os.remove(tmp_cs_path)

        # Copy các file phụ trợ vào thư mục
        shutil.copy2(VNC_PATH, os.path.join(temp_dir, "vncviewer.exe"))
        shutil.copy2(key_path, os.path.join(temp_dir, "client.key"))

        # Tạo file Hướng dẫn sử dụng
        readme_path = os.path.join(temp_dir, "HUONG_DAN_SU_DUNG.txt")
        with open(readme_path, "w", encoding="utf-8") as f:
            f.write(f"=== HUONG DAN VAO GAME 24/7 ===\n\n"
                    f"1. Giai nen toan bo thu muc nay ra Desktop hoac o dia C/D.\n"
                    f"2. Nhao dup vao file: '{title}.exe'.\n"
                    f"3. Bam nut '▶ VAO THAO TAC GAME (1 CLICK)' la man hinh game se hien len ngay lap tuc.\n"
                    f"4. Thao tac cai auto/treo acc xong, ban chi can bam [X] tat cua so game di.\n"
                    f"   Game tren VPS van tu dong cay 24/7!\n")

        # Nén lại thành ZIP
        with zipfile.ZipFile(out_zip_path, "w", zipfile.ZIP_DEFLATED) as z:
            for root, _, files in os.walk(temp_dir):
                for file in files:
                    full_p = os.path.join(root, file)
                    rel_p = os.path.relpath(full_p, temp_dir)
                    z.write(full_p, rel_p)

        print(f"[+] Da dong goi xong file ZIP Portable: {out_zip_path} ({os.path.getsize(out_zip_path) / 1024 / 1024:.1f} MB)")
    finally:
        shutil.rmtree(temp_dir, ignore_errors=True)

def main():
    parser = argparse.ArgumentParser(description="Đóng gói bộ điều khiển treo game J2ME cho khách hàng")
    parser.add_argument("--host", default="14.250.250.229", help="IP VPS cua khach (Mac dinh: 14.250.250.229)")
    parser.add_argument("--port", type=int, default=12724, help="Port NAT cua khach (Mac dinh: 12724)")
    parser.add_argument("--title", default="NSO GEMINI", help="Ten game/tieu de app (Mac dinh: NSO GEMINI)")
    parser.add_argument("--acc", default="acc1", help="Ten account dieu khien (Mac dinh: acc1)")
    parser.add_argument("--key", default="", help="Duong dan private key rieng cua khach (neu bo trong se tu dong sinh cap khoa moi)")
    parser.add_argument("--admin-key", default="~/.ssh/id_ed25519_jar", help="Khoa admin de day public key len VPS")
    parser.add_argument("--mode", choices=["exe", "zip"], default="exe", help="Dinh dang xuat: 'exe' hoac 'zip'")
    parser.add_argument("--out", default="", help="Duong dan file dau ra (.exe hoac .zip)")
    parser.add_argument("--push-pubkey", action="store_true", help="Tu dong day public key an toan len VPS cua khach")

    args = parser.parse_args()
    check_requirements()

    safe_title = args.title.replace(" ", "_")
    dist_dir = os.path.join(BASE_DIR, "dist_clients")
    os.makedirs(dist_dir, exist_ok=True)

    # Xác định hoặc sinh private key riêng cho khách (Tuyệt đối KHÔNG dùng admin key root)
    key_path = args.key
    pub_path = None
    if not key_path:
        safe_host = args.host.replace(".", "_").replace(":", "_")
        key_prefix = os.path.join(dist_dir, f"client_{safe_host}_{args.port}")
        key_path = f"{key_prefix}.key"
        pub_path = f"{key_prefix}.key.pub"
        if not os.path.exists(key_path):
            print(f"[*] Sinh cap khoa SSH Ed25519 rieng biet cho khach: {key_path}")
            key_path, pub_path = generate_key_pair(key_prefix)
        else:
            print(f"[*] Su dung khoa client rieng da tao: {key_path}")
    else:
        pub_path = f"{key_path}.pub"

    # Xử lý đẩy public key lên VPS nếu được yêu cầu
    if args.push_pubkey:
        pub_content = None
        if pub_path and os.path.exists(pub_path):
            with open(pub_path, "r", encoding="utf-8") as f:
                pub_content = f.read().strip()
        else:
            res = subprocess.run(["ssh-keygen", "-y", "-f", key_path], capture_output=True, text=True)
            if res.returncode == 0:
                pub_content = res.stdout.strip()

        if pub_content:
            push_client_pubkey_to_vps(args.host, args.port, pub_content, args.admin_key)
        else:
            print("[!] Khong the lay duoc noi dung public key de day len VPS!")

    # Đường dẫn xuất file
    out_path = args.out
    if args.mode == "exe":
        if not out_path:
            out_path = os.path.join(dist_dir, f"{safe_title}_Client.exe")
        build_standalone_exe(args.host, args.port, args.title, args.acc, key_path, out_path)
    else:
        if not out_path:
            out_path = os.path.join(dist_dir, f"{safe_title}_Portable.zip")
        build_zip_package(args.host, args.port, args.title, args.acc, key_path, out_path)

    print("\n" + "=" * 60)
    print(" BÀN GIAO CHO KHÁCH HÀNG THÀNH CÔNG!")
    print(f" File giao cho khách: {out_path}")
    print(f" Private Key đính kèm: {key_path} (Cô lập riêng, không có quyền root)")
    print(" Khách chỉ cần tải file về máy, nhấp đúp là vào thẳng game!")
    print("=" * 60)

if __name__ == "__main__":
    main()
