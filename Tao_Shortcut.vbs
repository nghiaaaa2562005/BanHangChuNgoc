Option Explicit
Dim WshShell, fso, currentDir, exePath, desktopDir, shortcut

Set WshShell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")

currentDir = fso.GetParentFolderName(WScript.ScriptFullName)
exePath = fso.BuildPath(currentDir, "BanHangChuNgoc.exe")

If Not fso.FileExists(exePath) Then
    MsgBox "Khong tim thay file BanHangChuNgoc.exe trong thu muc nay!", 16, "Loi"
    WScript.Quit
End If

desktopDir = WshShell.SpecialFolders("Desktop")
Set shortcut = WshShell.CreateShortcut(fso.BuildPath(desktopDir, "Phan Mem Ban Hang - Chu Ngoc.lnk"))
shortcut.TargetPath = exePath
shortcut.WorkingDirectory = currentDir
shortcut.Description = "Phan Mem Quan Ly Ban Hang - Chu Ngoc"
shortcut.IconLocation = exePath & ",0"
shortcut.Save

MsgBox "Da tao loi tat 'Phan Mem Ban Hang - Chu Ngoc' ra man hinh Desktop thanh cong!" & vbCrLf & vbCrLf & "Ban co the ra man hinh Desktop de mo phan mem.", 64, "Thanh Cong"
