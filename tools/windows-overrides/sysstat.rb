class Sysstat < Formula
  desc "Performance monitoring tools (iostat, mpstat, pidstat, sar)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

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
