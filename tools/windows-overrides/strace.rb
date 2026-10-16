class Strace < Formula
  desc "Trace a program's process, thread, DLL and exception events"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

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
