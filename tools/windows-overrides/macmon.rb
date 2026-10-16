class Macmon < Formula
  desc "Sudoless performance monitoring for CPU tiers, GPU and power"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "macmon", "macmon"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/macmon --version")
    assert_match "hw.nperflevels", shell_output("#{bin}/macmon debug 2>&1")
    assert_match "cpu_active_ratio", shell_output("#{bin}/macmon pipe -s 1 -i 200")
  end
end
