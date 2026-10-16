class Ntfs3g < Formula
  desc "NTFS mount and ntfsprogs tools on Windows' own NTFS driver"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "ntfs-3g", "ntfs-3g"
    windows_exe "libexec/wintools.exe", "lowntfs-3g", "lowntfs-3g"
    windows_exe "libexec/wintools.exe", "ntfs-3g.probe", "ntfs-3g.probe"
    windows_exe "libexec/wintools.exe", "ntfslabel", "ntfslabel"
    windows_exe "libexec/wintools.exe", "ntfsinfo", "ntfsinfo"
    windows_exe "libexec/wintools.exe", "ntfsls", "ntfsls"
    windows_exe "libexec/wintools.exe", "ntfscat", "ntfscat"
    windows_exe "libexec/wintools.exe", "ntfsfix", "ntfsfix"
    windows_exe "libexec/wintools.exe", "mkntfs", "mkntfs"
  end

  def caveats
    <<~EOS
      Windows' NTFS driver is the read-write driver: ntfs-3g gives a volume a mount point (a drive
      letter or an empty folder) and the ntfsprogs tools work on volumes through it. Devices are
      drive letters (E:) or \\?\Volume{...}\ paths; attach disk images with Mount-DiskImage first.
      ntfsinfo -m, ntfsfix, mkntfs and mounting need an administrator.
    EOS
  end

  test do
    assert_match "ntfs-3g", shell_output("#{bin}/ntfs-3g --version")
    system bin/"ntfs-3g.probe", "--readonly", "C:"
    shell_output("#{bin}/ntfslabel C:")
    assert_match "Windows", shell_output("#{bin}/ntfsls -F C:")
    assert_match "[fonts]", shell_output("#{bin}/ntfscat C: Windows/win.ini")
    assert_match "Dumping Inode", shell_output("#{bin}/ntfsinfo -F Windows/win.ini C:")
    plan = shell_output("#{bin}/mkntfs --no-action --fast --label Homebrew Z:")
    assert_match "Would run: Format-Volume -DriveLetter Z", plan
    assert_match "system volume", shell_output("#{bin}/mkntfs C: 2>&1", 1)
    assert_match "No change made", shell_output("#{bin}/ntfsfix --no-action C:")
  end
end
