class Macmon < Formula
  desc "Sudoless performance monitoring for CPU tiers, GPU and power"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "macmon", "macmon"
  end

  test do
    assert_match(/\d+\.\d+/, shell_output("#{bin}/macmon --version"))
    assert_match "hw.nperflevels", shell_output("#{bin}/macmon debug 2>&1")
    assert_match "cpu_active_ratio", shell_output("#{bin}/macmon pipe -s 1 -i 200")
  end
end
