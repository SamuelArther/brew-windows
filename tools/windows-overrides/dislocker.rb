class Dislocker < Formula
  desc "Unlock and read BitLocker volumes, and inspect BitLocker metadata"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "dislocker", "dislocker"
    windows_exe "libexec/wintools.exe", "dislocker-fuse", "dislocker-fuse"
    windows_exe "libexec/wintools.exe", "dislocker-file", "dislocker-file"
    windows_exe "libexec/wintools.exe", "dislocker-metadata", "dislocker-metadata"
    windows_exe "libexec/wintools.exe", "dislocker-find", "dislocker-find"
    windows_exe "libexec/wintools.exe", "dislocker-bek", "dislocker-bek"
  end

  test do
    system bin/"dislocker", "-h"
    (testpath/"plain.img").write "a" * 4096
    shell_output("#{bin}/dislocker-find #{testpath}/plain.img", 1)
    assert_match "no BitLocker signature", shell_output("#{bin}/dislocker-metadata -V #{testpath}/plain.img 2>&1", 1)
  end
end
