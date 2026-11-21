#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
PACKAGE CLIENT TOOL - ĐÓNG GÓI BỘ ĐIỀU KHIỂN 1-CLICK CHO KHÁCH HÀNG
Tự động hóa 100% việc tạo file thực thi duy nhất (.exe) hoặc gói ZIP Portable
giao cho khách chơi game mà không cần khách phải cấu hình bất cứ thứ gì.
"""

import os
import sys
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
        print(f"[!] Khong tim thay file vncviewer.exe tai: {VNC_PATH}")
        sys.exit(1)
    if not os.path.exists(TEMPLATE_CS):
        print(f"[!] Khong tim thay template C# tai: {TEMPLATE_CS}")
        sys.exit(1)

def generate_key_pair(output_prefix):
    """Sinh cap khoa SSH Ed25519 rieng cho khach hang"""
    priv_key = f"{output_prefix}.key"
    pub_key = f"{output_prefix}.key.pub"
    if os.path.exists(priv_key):
        os.remove(priv_key)
    if os.path.exists(pub_key):
        os.remove(pub_key)
    
    cmd = f'ssh-keygen -t ed25519 -N "" -f "{priv_key}" -C "client-access"'
    subprocess.run(cmd, shell=True, check=True, stdout=subprocess.DEVNULL)
    return priv_key, pub_key

def build_standalone_exe(host, port, title, key_path, out_exe_path):
    """Biên dịch file C# nhúng sẵn cấu hình, vncviewer và private key thành 1 file EXE duy nhất"""
    with open(TEMPLATE_CS, "r", encoding="utf-8") as f:
        code = f.read()
    
    # Thay thế thông số cấu hình
    code = code.replace('public const string VPS_HOST = "14.250.250.229";', f'public const string VPS_HOST = "{host}";')
    code = code.replace('public const int VPS_PORT = 12724;', f'public const int VPS_PORT = {port};')
    code = code.replace('public const string GAME_TITLE = "NSO GEMINI";', f'public const string GAME_TITLE = "{title}";')

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

def build_zip_package(host, port, title, key_path, out_zip_path):
    """Đóng gói dạng thư mục Portable ZIP gồm EXE + vncviewer + key"""
    temp_dir = tempfile.mkdtemp(prefix="client_pkg_")
    try:
        # Biên dịch file EXE nhẹ (không nhúng VNC)
        exe_path = os.path.join(temp_dir, f"{title}.exe")
        with open(TEMPLATE_CS, "r", encoding="utf-8") as f:
            code = f.read()
        code = code.replace('public const string VPS_HOST = "14.250.250.229";', f'public const string VPS_HOST = "{host}";')
        code = code.replace('public const int VPS_PORT = 12724;', f'public const int VPS_PORT = {port};')
        code = code.replace('public const string GAME_TITLE = "NSO GEMINI";', f'public const string GAME_TITLE = "{title}";')

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
    parser.add_argument("--key", default="", help="Duong dan den file private key (neu de trong se dung key mac dinh cua may hoac tao moi)")
    parser.add_argument("--mode", choices=["exe", "zip"], default="exe", help="Dinh dang xuat: 'exe' (1 file duy nhat nhung san VNC) hoac 'zip' (thu muc Portable)")
    parser.add_argument("--out", default="", help="Duong dan file dau ra (.exe hoac .zip)")
    parser.add_argument("--push-pubkey", action="store_true", help="Tu dong day public key len VPS cua khach")

    args = parser.parse_args()
    check_requirements()

    # Xác định private key
    key_path = args.key
    if not key_path:
        default_key = os.path.expanduser(r"~/.ssh/id_ed25519_jar")
        if os.path.exists(default_key):
            key_path = default_key
        else:
            print("[*] Sinh cap khoa SSH moi cho khach...")
            key_path, pub_path = generate_key_pair(os.path.join(BASE_DIR, "tools", "client"))
            print(f"[+] Key moi: {key_path}")

    # Đường dẫn xuất file
    out_path = args.out
    safe_title = args.title.replace(" ", "_")
    dist_dir = os.path.join(BASE_DIR, "dist_clients")
    os.makedirs(dist_dir, exist_ok=True)

    if args.mode == "exe":
        if not out_path:
            out_path = os.path.join(dist_dir, f"{safe_title}_Client.exe")
        build_standalone_exe(args.host, args.port, args.title, key_path, out_path)
    else:
        if not out_path:
            out_path = os.path.join(dist_dir, f"{safe_title}_Portable.zip")
        build_zip_package(args.host, args.port, args.title, key_path, out_path)

    print("\n" + "=" * 60)
    print(" BÀN GIAO CHO KHÁCH HÀNG THÀNH CÔNG!")
    print(f" File giao cho khách: {out_path}")
    print(" Khách chỉ cần tải file này về máy, nhấp đúp là vào thẳng game!")
    print("=" * 60)

if __name__ == "__main__":
    main()
