class Strace < Formula
  desc "Trace a program's process, thread, DLL and exception events"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "strace", "strace"
  end

  test do
    assert_match(/\d+\.\d+/, shell_output("#{bin}/strace -V"))
    out = shell_output("#{bin}/strace cmd /c \"exit 0\" 2>&1")
    assert_match "execve(", out
    assert_match "+++ exited with 0 +++", out
    assert_match "total", shell_output("#{bin}/strace -c cmd /c \"exit 0\" 2>&1")
  end
end
