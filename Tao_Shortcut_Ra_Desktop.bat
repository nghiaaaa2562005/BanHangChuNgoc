@echo off
chcp 65001 >nul
title Tạo Shortcut Ra Màn Hình Desktop - Bán Hàng Chú Ngọc
echo ====================================================================
echo      ĐANG TẠO PHÍM TẮT (SHORTCUT) RA MÀN HÌNH CHÍNH DESKTOP...
echo ====================================================================
echo.

set "CURRENT_DIR=%~dp0"
set "TARGET_EXE=%CURRENT_DIR%BanHangChuNgoc.exe"

if not exist "%TARGET_EXE%" (
    echo [LỖI] Không tìm thấy file BanHangChuNgoc.exe trong thư mục này!
    echo Vui lòng đảm bảo file này nằm chung thư mục với file BanHangChuNgoc.exe.
    echo.
    pause
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -Command "$ws = New-Object -ComObject WScript.Shell; $desktop = [System.Environment]::GetFolderPath('Desktop'); $shortcut = $ws.CreateShortcut((Join-Path $desktop 'Phần Mềm Bán Hàng - Chú Ngọc.lnk')); $shortcut.TargetPath = '%TARGET_EXE%'; $shortcut.WorkingDirectory = '%CURRENT_DIR%'; $shortcut.Description = 'Phần mềm Quản lý Bán Hàng Quần Áo - Chú Ngọc'; $shortcut.IconLocation = '%TARGET_EXE%,0'; $shortcut.Save()"

if %ERRORLEVEL% EQU 0 (
    echo [THÀNH CÔNG] Đã tạo biểu tượng "Phần Mềm Bán Hàng - Chú Ngọc" trên Desktop!
    echo Bạn có thể ra màn hình chính Desktop để mở phần mềm bất cứ lúc nào.
) else (
    echo [LỖI] Không thể tạo shortcut tự động. Bạn có thể bấm chuột phải vào BanHangChuNgoc.exe chọn Send to -^> Desktop (create shortcut).
)

echo.
echo Nhấn phím bất kỳ để thoát...
pause >nul
