#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
GIT HISTORY SCRUB TOOL - LÀM SẠCH LỊCH SỬ COMMIT TRƯỚC KHI PUSH GITHUB
Công cụ hỗ trợ xóa bỏ chuỗi nhạy cảm khỏi toàn bộ lịch sử Git bằng git-filter-repo.
TUYỆT ĐỐI KHÔNG HARDCODE MẬT KHẨU HAY BẤT KỲ THÔNG TIN NHẠY CẢM NÀO VÀO FILE NÀY.
"""

import os
import sys
import argparse
import subprocess
import tempfile

def check_git_filter_repo():
    res = subprocess.run(["git", "filter-repo", "--version"], capture_output=True, text=True)
    if res.returncode != 0:
        print("[!] Chưa cài đặt git-filter-repo.")
        print("    Vui lòng cài đặt bằng lệnh: pip install git-filter-repo")
        return False
    return True

def scrub_history(patterns_file=None, search_str=None, replace_str="<ROOT_PASSWORD>"):
    print("=" * 65)
    print(" CÔNG CỤ LÀM SẠCH LỊCH SỬ GIT (SCRUB SENSITIVE STRINGS)")
    print("=" * 65)

    if not check_git_filter_repo():
        return False

    temp_created = False
    expr_path = None

    if patterns_file:
        if not os.path.exists(patterns_file):
            print(f"[!] File quy tắc không tồn tại: {patterns_file}")
            return False
        expr_path = patterns_file
    elif search_str:
        with tempfile.NamedTemporaryFile("w", delete=False, encoding="utf-8") as f:
            f.write(f"{search_str}==>{replace_str}\n")
            expr_path = f.name
            temp_created = True
    else:
        print("[*] Nhập chuỗi nhạy cảm cần xóa khỏi toàn bộ lịch sử Git:")
        user_input = input("Chuỗi cần xóa: ").strip()
        if not user_input:
            print("[!] Bạn chưa nhập chuỗi cần xóa. Đã hủy thao tác.")
            return False
        user_replace = input("Chuỗi thay thế (Mặc định: <ROOT_PASSWORD>): ").strip() or "<ROOT_PASSWORD>"
        with tempfile.NamedTemporaryFile("w", delete=False, encoding="utf-8") as f:
            f.write(f"{user_input}==>{user_replace}\n")
            expr_path = f.name
            temp_created = True

    try:
        print("[*] Đang tiến hành làm sạch lịch sử Git bằng git-filter-repo...")
        cmd = ["git", "filter-repo", "--replace-text", expr_path, "--force"]
        res = subprocess.run(cmd, capture_output=True, text=True)
        if res.returncode == 0:
            print("[+] Đã làm sạch lịch sử Git thành công!")
            print("    Kho lưu trữ hiện đã an toàn để push lên GitHub.")
            return True
        else:
            print("[!] Quá trình scrub thất bại:\n", res.stderr or res.stdout)
            return False
    finally:
        if temp_created and expr_path and os.path.exists(expr_path):
            os.remove(expr_path)

def main():
    parser = argparse.ArgumentParser(description="Làm sạch lịch sử Git bằng git-filter-repo")
    parser.add_argument("--file", help="Đường dẫn file chứa các quy tắc dạng 'old==>new'")
    parser.add_argument("--search", help="Chuỗi nhạy cảm cần xóa")
    parser.add_argument("--replace", default="<ROOT_PASSWORD>", help="Chuỗi thay thế (Mặc định: <ROOT_PASSWORD>)")
    args = parser.parse_args()

    scrub_history(patterns_file=args.file, search_str=args.search, replace_str=args.replace)

if __name__ == "__main__":
    main()
