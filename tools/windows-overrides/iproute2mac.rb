class Iproute2mac < Formula
  desc "CLI for basic network utilities on Windows - ip command"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "ip", "ip"
    windows_exe "libexec/wintools.exe", "ss", "ss"
    windows_exe "libexec/wintools.exe", "bridge", "bridge"
  end

  test do
    system bin/"ip", "route"
    system bin/"ip", "address"
    system bin/"ip", "neigh"
    system bin/"bridge", "link"
    system bin/"ss"
    assert_match "link/loopback", shell_output("#{bin}/ip link")
  end
end
