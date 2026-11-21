#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Tự động tải vncviewer.exe (TigerVNC Viewer) chính thức nếu chưa có trong dự án.
"""

import os
import sys
import urllib.request

BASE_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TARGET_PATH = os.path.join(BASE_DIR, "vncviewer.exe")
TIGERVNC_URL = "https://github.com/TigerVNC/tigervnc/releases/download/v1.13.1/vncviewer64-1.13.1.exe"

def ensure_vncviewer():
    if os.path.exists(TARGET_PATH) and os.path.getsize(TARGET_PATH) > 0:
        print(f"[+] vncviewer.exe da co san tai: {TARGET_PATH} ({os.path.getsize(TARGET_PATH) / 1024 / 1024:.1f} MB)")
        return True

    print(f"[*] Dang tai vncviewer.exe tu TigerVNC official release...")
    try:
        urllib.request.urlretrieve(TIGERVNC_URL, TARGET_PATH)
        print(f"[+] Da tai xong vncviewer.exe: {TARGET_PATH}")
        return True
    except Exception as e:
        print(f"[!] Khong the tai vncviewer.exe: {e}")
        return False

if __name__ == "__main__":
    ensure_vncviewer()
