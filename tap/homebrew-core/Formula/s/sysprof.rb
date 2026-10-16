class Sysprof < Formula
  desc "Statistical, system-wide profiler built on the Windows Performance Recorder"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "sysprof", "sysprof"
    windows_exe "libexec/wintools.exe", "sysprof-cli", "sysprof-cli"
  end

  test do
    assert_match(/\d+\.\d+/, shell_output("#{bin}/sysprof --version"))
    assert_match "Windows Performance Recorder", shell_output("#{bin}/sysprof --help")
    assert_match(/WPR|recording/i, shell_output("#{bin}/sysprof status 2>&1"))
  end
end
