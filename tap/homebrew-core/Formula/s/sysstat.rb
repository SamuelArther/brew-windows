class Sysstat < Formula
  desc "Performance monitoring tools (iostat, mpstat, pidstat, sar)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "iostat", "iostat"
    windows_exe "libexec/wintools.exe", "mpstat", "mpstat"
    windows_exe "libexec/wintools.exe", "pidstat", "pidstat"
    windows_exe "libexec/wintools.exe", "sar", "sar"
  end

  test do
    assert_match "PID", shell_output("#{bin}/pidstat")
    assert_match "avg-cpu", shell_output("#{bin}/iostat")
    assert_match "%idle", shell_output("#{bin}/mpstat")
    system bin/"sar", "-o", testpath/"sa", "1", "2"
    assert_match "Average:", shell_output("#{bin}/sar -f #{testpath}/sa -u")
  end
end
