class Iputils < Formula
  desc "Set of small useful utilities for networking (ping, tracepath, arping)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "ping", "ping"
    windows_exe "libexec/wintools.exe", "tracepath", "tracepath"
    windows_exe "libexec/wintools.exe", "arping", "arping"
  end

  test do
    assert_match "iputils", shell_output("#{bin}/ping -V")
    assert_match "1 received", shell_output("#{bin}/ping -c 1 127.0.0.1")
    assert_match "reached", shell_output("#{bin}/tracepath -n 127.0.0.1")
    assert_match "iputils", shell_output("#{bin}/arping -V")
    assert_match "Name or service not known", shell_output("#{bin}/ping -c 1 nonexistent.invalid 2>&1", 2)
  end
end
