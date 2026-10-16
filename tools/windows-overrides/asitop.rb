class Asitop < Formula
  desc "Perf monitoring CLI tool for CPU tiers, GPU, power and memory"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "asitop", "asitop"
  end

  test do
    assert_match "Performance monitoring CLI tool", shell_output("#{bin}/asitop --help")
    assert_match "RAM Usage", shell_output("#{bin}/asitop --max_count 1")
  end
end
