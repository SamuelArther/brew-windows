class Sshfs < Formula
  desc "File system client based on SSH File Transfer Protocol"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "sshfs", "sshfs"
  end

  test do
    assert_match "SSHFS version", shell_output("#{bin}/sshfs --version")
    assert_match "IdentityFile", shell_output("#{bin}/sshfs --help")
    assert_match "missing host", shell_output("#{bin}/sshfs nohost #{testpath}/mnt 2>&1", 2)
  end
end
