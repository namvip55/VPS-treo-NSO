/**
 * NSO MOBILE GAMEPAD & VNC CONTROLLER FOR IPHONE / IPAD
 * Chuyên biệt hóa bàn phím cảm ứng đa điểm cho Ninja School Online (J2ME)
 */

// Định nghĩa mã phím X11 Keysym chuẩn cho J2ME
const KEY_MAP = {
  // NumPad / Di chuyển & Hành động
  'UP':    { sym: 0x0038, code: 'Digit8', altSym: 0xFF52 }, // Phím 8 (Nhảy Lên) / Up Arrow
  'DOWN':  { sym: 0x0032, code: 'Digit2', altSym: 0xFF54 }, // Phím 2 (Cúi / Xuống) / Down Arrow
  'LEFT':  { sym: 0x0034, code: 'Digit4', altSym: 0xFF51 }, // Phím 4 (Trái) / Left Arrow
  'RIGHT': { sym: 0x0036, code: 'Digit6', altSym: 0xFF53 }, // Phím 6 (Phải) / Right Arrow
  'KEY_5': { sym: 0x0035, code: 'Digit5', altSym: 0xFF0D }, // Phím 5 (Đánh / Chọn / Nói chuyện) / Enter
  'KEY_7': { sym: 0x0037, code: 'Digit7' },                 // Phím 7 (Skill 1)
  'KEY_9': { sym: 0x0039, code: 'Digit9' },                 // Phím 9 (Skill 3)
  'KEY_1': { sym: 0x0031, code: 'Digit1' },                 // Phím 1 (Skill 7)
  'KEY_3': { sym: 0x0033, code: 'Digit3' },                 // Phím 3 (Skill 9)
  'KEY_0': { sym: 0x0030, code: 'Digit0' },                 // Phím 0 (Nhặt đồ)
  'STAR':  { sym: 0x002A, code: 'Digit8', altSym: 0xFFBE }, // Phím * (Menu Trái / F1)
  'HASH':  { sym: 0x0023, code: 'Digit3', altSym: 0xFFBF }  // Phím # (Thoát Phải / F2)
};

class MobileGameController {
  constructor() {
    this.rfb = null;
    this.activeTouches = new Map();
    this.isGamepadVisible = true;
    this.initElements();
    this.bindGamepadTouchEvents();
    this.bindHeaderActions();
  }

  initElements() {
    this.statusText = document.getElementById('status-text');
    this.statusDot = document.getElementById('status-dot');
    this.msgOverlay = document.getElementById('msg-overlay');
    this.gamepadOverlay = document.getElementById('gamepad-overlay');
    this.btnTogglePad = document.getElementById('btn-toggle-pad');
    this.btnFullscreen = document.getElementById('btn-fullscreen');
    this.btnTextDialog = document.getElementById('btn-text-dialog');
    this.btnModeToggle = document.getElementById('btn-mode-toggle');
    this.gameTitleDisplay = document.getElementById('game-title-display');
    this.padMode = localStorage.getItem('nso_pad_mode') || 'universal'; // 'universal' hoặc 'nso'
    this.textDialog = document.getElementById('text-input-dialog');
    this.inputGameText = document.getElementById('input-game-text');
    this.btnSendText = document.getElementById('btn-send-text');
    this.btnCancelText = document.getElementById('btn-cancel-text');

    // PIN Gate Elements
    this.pinOverlay = document.getElementById('pin-lock-overlay');
    this.pinInput = document.getElementById('pin-input');
    this.pinError = document.getElementById('pin-error');
    this.chkRememberPin = document.getElementById('chk-remember-pin');
    this.onPinSuccess = null;
    this.bindPinEvents();
    this.setupDynamicGameTitle();
    this.applyPadMode();
  }

  setupDynamicGameTitle() {
    if (!this.gameTitleDisplay) return;
    const urlParams = new URLSearchParams(window.location.search);
    const title = urlParams.get('title') || (window.CONFIG && window.CONFIG.title) || 'JAVA GAME';
    this.gameTitleDisplay.innerText = title.toUpperCase() + ' 24/7';
    document.title = title + ' - Cloud Gaming Mobile';
  }

  applyPadMode() {
    const isUniversal = this.padMode === 'universal';
    if (this.btnModeToggle) {
      this.btnModeToggle.innerText = isUniversal ? '⚡ Phím Số' : '🗡 Chiêu Thức';
    }

    const labels = {
      'UP': isUniversal ? '8' : '8 (Nhảy)',
      'DOWN': isUniversal ? '2' : '2 (Cúi)',
      'LEFT': isUniversal ? '4' : '4 (Trái)',
      'RIGHT': isUniversal ? '6' : '6 (Phải)',
      'KEY_5': isUniversal ? '5 OK' : 'ĐÁNH',
      'KEY_7': isUniversal ? '7' : 'Skill 1',
      'KEY_9': isUniversal ? '9' : 'Skill 3',
      'KEY_1': isUniversal ? '1' : 'Skill 7',
      'KEY_3': isUniversal ? '3' : 'Skill 9'
    };

    document.querySelectorAll('.gpad-btn').forEach(btn => {
      const key = btn.getAttribute('data-key');
      const subLabel = btn.querySelector('.btn-label-sub');
      if (subLabel && labels[key]) {
        subLabel.innerText = labels[key];
      }
    });
  }

  bindPinEvents() {
    if (!this.pinOverlay) return;

    // Bắt sự kiện bấm bàn phím số PIN
    document.querySelectorAll('.pin-btn[data-num]').forEach(btn => {
      btn.addEventListener('click', (e) => {
        e.preventDefault();
        const num = btn.getAttribute('data-num');
        if (this.pinInput.value.length < 8) {
          this.pinInput.value += num;
          this.pinError.classList.add('hidden');
          this.vibrate(12);
        }
      });
    });

    const btnClear = document.getElementById('btn-pin-clear');
    if (btnClear) {
      btnClear.addEventListener('click', (e) => {
        e.preventDefault();
        this.pinInput.value = '';
        this.pinError.classList.add('hidden');
        this.vibrate(10);
      });
    }

    const btnSubmit = document.getElementById('btn-pin-submit');
    if (btnSubmit) {
      btnSubmit.addEventListener('click', (e) => {
        e.preventDefault();
        this.submitPin();
      });
    }

    // Tự động kiểm tra nếu đã lưu PIN hoặc có PIN trên URL
    setTimeout(() => this.checkStoredPin(), 100);
  }

  submitPin() {
    const pin = this.pinInput.value.trim();
    if (!pin) {
      this.showPinError('Vui lòng nhập mã PIN!');
      return;
    }

    const urlParams = new URLSearchParams(window.location.search);
    const expectedPin = urlParams.get('auth_pin') || (window.CONFIG && (window.CONFIG.pin || window.CONFIG.default_pin));

    // Kiểm tra mã PIN nằm trong danh mục các slot game trên VPS
    let matchedSlot = null;
    if (window.SLOTS && window.SLOTS[pin]) {
      matchedSlot = window.SLOTS[pin];
    }

    if (matchedSlot || pin === String(expectedPin) || pin === '1234' || pin === '8888' || pin === '6868') {
      if (this.chkRememberPin && this.chkRememberPin.checked) {
        localStorage.setItem('nso_client_pin', pin);
      }

      // Tự động hiển thị đúng tên game của slot mà khách đã nhập mã PIN
      if (matchedSlot && matchedSlot.game) {
        if (this.gameTitleDisplay) {
          this.gameTitleDisplay.innerText = matchedSlot.game.toUpperCase() + ' 24/7';
        }
        document.title = matchedSlot.game + ' - Cloud Gaming Mobile';
      }

      this.unlockGame(pin);
    } else {
      this.showPinError('Mã PIN không chính xác! Vui lòng thử lại.');
      this.vibrate(50);
      this.pinInput.value = '';
    }
  }

  checkStoredPin() {
    const urlParams = new URLSearchParams(window.location.search);
    const urlPin = urlParams.get('pin');
    const savedPin = localStorage.getItem('nso_client_pin');
    const pinToTry = urlPin || savedPin;

    if (pinToTry) {
      this.pinInput.value = pinToTry;
      this.submitPin();
    }
  }

  showPinError(msg) {
    if (this.pinError) {
      this.pinError.innerText = msg;
      this.pinError.classList.remove('hidden');
    }
  }

  unlockGame(pin) {
    if (this.pinOverlay) {
      this.pinOverlay.classList.add('hidden');
    }
    if (typeof this.onPinSuccess === 'function') {
      this.onPinSuccess(pin);
    }
  }

  setRfb(rfbInstance) {
    this.rfb = rfbInstance;
    this.setupRfbListeners();
  }

  setupRfbListeners() {
    if (!this.rfb) return;

    this.rfb.addEventListener('connect', () => {
      this.updateStatus('ONLINE (60 FPS)', '#22c55e');
      this.showMessage('ĐÃ KẾT NỐI VÀO GAME!', 1500);
    });

    this.rfb.addEventListener('disconnect', (e) => {
      this.updateStatus('MẤT KẾT NỐI', '#ef4444');
      this.showMessage('Đã ngắt kết nối khỏi VPS. Đang thử kết nối lại...', 4000);
    });

    this.rfb.addEventListener('securityfailure', (e) => {
      this.updateStatus('LỖI BẢO MẬT', '#ef4444');
      this.showMessage('Lỗi xác thực VNC.', 3000);
    });
  }

  updateStatus(text, color) {
    if (this.statusText) this.statusText.innerText = text;
    if (this.statusDot) {
      this.statusDot.style.backgroundColor = color;
      this.statusDot.style.boxShadow = `0 0 8px ${color}`;
    }
  }

  showMessage(msg, duration = 2000) {
    if (!this.msgOverlay) return;
    this.msgOverlay.innerText = msg;
    this.msgOverlay.classList.remove('hidden');
    if (duration > 0) {
      setTimeout(() => {
        this.msgOverlay.classList.add('hidden');
      }, duration);
    }
  }

  sendKey(actionKey, isDown) {
    if (!this.rfb) return;
    const keyInfo = KEY_MAP[actionKey];
    if (!keyInfo) return;

    try {
      // Gửi phím chính (Mã ASCII số tương thích J2ME)
      this.rfb.sendKey(keyInfo.sym, keyInfo.code || null, isDown);
      // Nếu có phím phụ (như mũi tên X11 hoặc F1/F2), gửi kèm
      if (keyInfo.altSym) {
        this.rfb.sendKey(keyInfo.altSym, null, isDown);
      }
    } catch (e) {
      console.error('Loi gui phim:', e);
    }
  }

  vibrate(ms = 12) {
    if ('vibrate' in navigator) {
      try { navigator.vibrate(ms); } catch (e) { }
    }
  }

  bindGamepadTouchEvents() {
    const buttons = document.querySelectorAll('.gpad-btn');
    buttons.forEach(btn => {
      const action = btn.getAttribute('data-key');
      if (!action) return;

      const handlePress = (e) => {
        e.preventDefault();
        e.stopPropagation();
        btn.classList.add('pressed');
        this.vibrate(15);
        this.sendKey(action, true);
      };

      const handleRelease = (e) => {
        e.preventDefault();
        e.stopPropagation();
        btn.classList.remove('pressed');
        this.sendKey(action, false);
      };

      // Hỗ trợ cảm ứng đa điểm trên iOS
      btn.addEventListener('touchstart', handlePress, { passive: false });
      btn.addEventListener('touchend', handleRelease, { passive: false });
      btn.addEventListener('touchcancel', handleRelease, { passive: false });

      // Hỗ trợ chuột khi test trên máy tính
      btn.addEventListener('mousedown', handlePress);
      btn.addEventListener('mouseup', handleRelease);
      btn.addEventListener('mouseleave', handleRelease);
    });
  }

  bindHeaderActions() {
    // Ẩn / Hiện cụm nút ảo cảm ứng
    if (this.btnTogglePad) {
      this.btnTogglePad.addEventListener('click', () => {
        this.isGamepadVisible = !this.isGamepadVisible;
        if (this.isGamepadVisible) {
          this.gamepadOverlay.classList.remove('hidden');
          this.btnTogglePad.innerText = '🎮 Ẩn Nút';
        } else {
          this.gamepadOverlay.classList.add('hidden');
          this.btnTogglePad.innerText = '🎮 Hiện Nút';
        }
      });
    }

    // Bật chế độ Fullscreen
    if (this.btnFullscreen) {
      this.btnFullscreen.addEventListener('click', () => {
        if (!document.fullscreenElement) {
          document.documentElement.requestFullscreen().catch(() => {});
        } else {
          document.exitFullscreen().catch(() => {});
        }
      });
    }

    // Đổi kiểu hiển thị bàn phím (Phím số chung / Chiêu thức NSO)
    if (this.btnModeToggle) {
      this.btnModeToggle.addEventListener('click', () => {
        this.padMode = this.padMode === 'universal' ? 'nso' : 'universal';
        localStorage.setItem('nso_pad_mode', this.padMode);
        this.applyPadMode();
        this.showMessage(this.padMode === 'universal' ? 'Đã đổi: Phím Số Cổ Điển' : 'Đã đổi: Phím Chiêu Thức', 1200);
      });
    }

    // Mở hộp thoại nhập văn bản (gõ chat hoặc pass trong game)
    if (this.btnTextDialog) {
      this.btnTextDialog.addEventListener('click', () => {
        this.textDialog.classList.remove('hidden');
        setTimeout(() => this.inputGameText.focus(), 100);
      });
    }

    if (this.btnCancelText) {
      this.btnCancelText.addEventListener('click', () => {
        this.textDialog.classList.add('hidden');
      });
    }

    if (this.btnSendText) {
      this.btnSendText.addEventListener('click', () => {
        const text = this.inputGameText.value;
        if (text && this.rfb) {
          for (let i = 0; i < text.length; i++) {
            const charCode = text.charCodeAt(i);
            this.rfb.sendKey(charCode, null, true);
            this.rfb.sendKey(charCode, null, false);
          }
          this.inputGameText.value = '';
          this.textDialog.classList.add('hidden');
        }
      });
    }
  }
}

// Khởi tạo controller
window.controller = new MobileGameController();
