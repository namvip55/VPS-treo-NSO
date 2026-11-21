Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

[System.Windows.Forms.Application]::EnableVisualStyles()

# Global variables
$global:TunnelProcess = $null
$global:VpsHost = "remote.io.vn:12724"

function Invoke-Vps {
    param([string]$cmd)
    try {
        $pinfo = New-Object System.Diagnostics.ProcessStartInfo
        $pinfo.FileName = "ssh"
        $pinfo.Arguments = "vps-jar `"$cmd`""
        $pinfo.RedirectStandardOutput = $true
        $pinfo.RedirectStandardError = $true
        $pinfo.UseShellExecute = $false
        $pinfo.CreateNoWindow = $true

        $p = New-Object System.Diagnostics.Process
        $p.StartInfo = $pinfo
        $null = $p.Start()
        $stdout = $p.StandardOutput.ReadToEnd()
        $null = $p.WaitForExit(5000)
        return $stdout.Trim()
    } catch {
        return ""
    }
}

function Ensure-Tunnel {
    $hasTunnel = Get-NetTCPConnection -LocalPort 5901 -ErrorAction SilentlyContinue
    if (-not $hasTunnel) {
        $tunnelStart = New-Object System.Diagnostics.ProcessStartInfo
        $tunnelStart.FileName = "ssh"
        $tunnelStart.Arguments = "-N -L 5901:localhost:5901 vps-jar"
        $tunnelStart.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Hidden
        $tunnelStart.CreateNoWindow = $true
        $global:TunnelProcess = [System.Diagnostics.Process]::Start($tunnelStart)
        Start-Sleep -Milliseconds 600
    }
}

function Open-GameScreen {
    Ensure-Tunnel
    $vncPath = "C:\JarManager\vncviewer.exe"
    if (-not (Test-Path $vncPath)) {
        $vncPath = Join-Path $PSScriptRoot "vncviewer.exe"
    }
    if (Test-Path $vncPath) {
        $vncArgs = "localhost:5901 -PreferredEncoding=Tight -QualityLevel=6 -CompressLevel=1 -AutoSelect=0 -CustomCompressLevel=1"
        Start-Process -FilePath $vncPath -ArgumentList $vncArgs
    } else {
        [System.Windows.Forms.MessageBox]::Show("Khong tim thay file vncviewer.exe!", "Loi", "OK", "Error")
    }
}

# --- TẠO GIAO DIỆN TỐI GIẢN 1 CỬA SỔ DUY NHẤT ---
$form = New-Object System.Windows.Forms.Form
$form.Text = "JarManager - Treo Game NSO 24/7"
$form.Size = New-Object System.Drawing.Size(460, 390)
$form.StartPosition = "CenterScreen"
$form.FormBorderStyle = "FixedDialog"
$form.MaximizeBox = $false
$form.BackColor = [System.Drawing.Color]::FromArgb(246, 248, 251)
$form.Font = New-Object System.Drawing.Font("Segoe UI", 9.5)

# --- HEADER STATUS ---
$pnlHeader = New-Object System.Windows.Forms.Panel
$pnlHeader.Location = New-Object System.Drawing.Point(15, 12)
$pnlHeader.Size = New-Object System.Drawing.Size(415, 60)
$pnlHeader.BackColor = [System.Drawing.Color]::White
$pnlHeader.BorderStyle = [System.Windows.Forms.BorderStyle]::FixedSingle
$form.Controls.Add($pnlHeader)

$lblStatus = New-Object System.Windows.Forms.Label
$lblStatus.Text = "[●] CLOUD VPS: ONLINE"
$lblStatus.Font = New-Object System.Drawing.Font("Segoe UI", 11, [System.Drawing.FontStyle]::Bold)
$lblStatus.ForeColor = [System.Drawing.Color]::FromArgb(22, 163, 74)
$lblStatus.Location = New-Object System.Drawing.Point(12, 8)
$lblStatus.AutoSize = $true
$pnlHeader.Controls.Add($lblStatus)

$lblSub = New-Object System.Windows.Forms.Label
$lblSub.Text = "remote.io.vn:12724 | NSO GEMINI Server + Client 24/7"
$lblSub.ForeColor = [System.Drawing.Color]::FromArgb(100, 116, 139)
$lblSub.Location = New-Object System.Drawing.Point(13, 31)
$lblSub.AutoSize = $true
$pnlHeader.Controls.Add($lblSub)

# --- NÚT VÀO GAME LỚN DUY NHẤT ---
$btnOpenGame = New-Object System.Windows.Forms.Button
$btnOpenGame.Text = "▶ VAO THAO TAC GAME (1 CLICK)"
$btnOpenGame.Location = New-Object System.Drawing.Point(15, 85)
$btnOpenGame.Size = New-Object System.Drawing.Size(415, 60)
$btnOpenGame.BackColor = [System.Drawing.Color]::FromArgb(22, 163, 74)
$btnOpenGame.ForeColor = [System.Drawing.Color]::White
$btnOpenGame.Font = New-Object System.Drawing.Font("Segoe UI", 12.5, [System.Drawing.FontStyle]::Bold)
$btnOpenGame.Cursor = [System.Windows.Forms.Cursors]::Hand
$btnOpenGame.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
$form.Controls.Add($btnOpenGame)

# --- KHỐI ĐIỀU KHIỂN PHỤ (Khởi động lại / Tắt) ---
$pnlControls = New-Object System.Windows.Forms.Panel
$pnlControls.Location = New-Object System.Drawing.Point(15, 155)
$pnlControls.Size = New-Object System.Drawing.Size(415, 42)
$form.Controls.Add($pnlControls)

$btnRestart = New-Object System.Windows.Forms.Button
$btnRestart.Text = "↺ Khoi Dong Lai Game"
$btnRestart.Location = New-Object System.Drawing.Point(0, 0)
$btnRestart.Size = New-Object System.Drawing.Size(200, 38)
$btnRestart.BackColor = [System.Drawing.Color]::FromArgb(235, 240, 245)
$btnRestart.Cursor = [System.Windows.Forms.Cursors]::Hand
$pnlControls.Controls.Add($btnRestart)

$btnStop = New-Object System.Windows.Forms.Button
$btnStop.Text = "■ Tat Game"
$btnStop.Location = New-Object System.Drawing.Point(215, 0)
$btnStop.Size = New-Object System.Drawing.Size(200, 38)
$btnStop.BackColor = [System.Drawing.Color]::FromArgb(254, 226, 226)
$btnStop.ForeColor = [System.Drawing.Color]::FromArgb(185, 28, 28)
$btnStop.Cursor = [System.Windows.Forms.Cursors]::Hand
$pnlControls.Controls.Add($btnStop)

# --- KHUNG HƯỚNG DẪN BÀN PHÍM & TREO 24/7 ---
$pnlHint = New-Object System.Windows.Forms.Panel
$pnlHint.Location = New-Object System.Drawing.Point(15, 208)
$pnlHint.Size = New-Object System.Drawing.Size(415, 125)
$pnlHint.BackColor = [System.Drawing.Color]::FromArgb(240, 253, 244)
$pnlHint.BorderStyle = [System.Windows.Forms.BorderStyle]::FixedSingle
$form.Controls.Add($pnlHint)

$lblHintTitle = New-Object System.Windows.Forms.Label
$lblHintTitle.Text = "• Huong Dan Dieu Khien & Treo 24/7:"
$lblHintTitle.Font = New-Object System.Drawing.Font("Segoe UI", 9.5, [System.Drawing.FontStyle]::Bold)
$lblHintTitle.ForeColor = [System.Drawing.Color]::FromArgb(21, 128, 61)
$lblHintTitle.Location = New-Object System.Drawing.Point(10, 8)
$lblHintTitle.AutoSize = $true
$pnlHint.Controls.Add($lblHintTitle)

$lblHintBody = New-Object System.Windows.Forms.Label
$lblHintBody.Text = "• Ban phim so NumPad (Da sua chuan 100%):`n  - Phim 8: Nhay Len | Phim 2: Cui / Di Xuong`n  - Phim 4: Sang Trai | Phim 6: Sang Phai`n  - Phim 5: Danh thuong / Chon | Phim 7, 9: Skill`n• Thao tac game xong, ban chi can bam [X] tat cua so game di.`n✔ Game tren VPS van tu dong cay 24/7 binh thuong!"
$lblHintBody.Font = New-Object System.Drawing.Font("Segoe UI", 8.8)
$lblHintBody.ForeColor = [System.Drawing.Color]::FromArgb(30, 41, 59)
$lblHintBody.Location = New-Object System.Drawing.Point(10, 28)
$lblHintBody.AutoSize = $true
$pnlHint.Controls.Add($lblHintBody)

# --- EVENT HANDLERS ---
$btnOpenGame.Add_Click({
    Open-GameScreen
})

$btnRestart.Add_Click({
    Invoke-Vps "/opt/jar-manager/scripts/stop-acc.sh acc1" | Out-Null
    Start-Sleep -Milliseconds 500
    Invoke-Vps "/opt/jar-manager/scripts/start-acc.sh acc1" | Out-Null
    Start-Sleep -Milliseconds 800
    Open-GameScreen
})

$btnStop.Add_Click({
    $confirm = [System.Windows.Forms.MessageBox]::Show("Ban co chac muon tat game dang chay tren VPS?", "Xac nhan", "YesNo", "Question")
    if ($confirm -eq "Yes") {
        Invoke-Vps "/opt/jar-manager/scripts/stop-acc.sh acc1" | Out-Null
        [System.Windows.Forms.MessageBox]::Show("Da dung game tren VPS!", "Thong bao", "OK", "Information")
    }
})

$form.Add_FormClosing({
    if ($global:TunnelProcess -and -not $global:TunnelProcess.HasExited) {
        $global:TunnelProcess.Kill()
    }
    Get-Process -Name "ssh" -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -like "*5901:localhost:5901*" } | Stop-Process -Force -ErrorAction SilentlyContinue
})

[void]$form.ShowDialog()
