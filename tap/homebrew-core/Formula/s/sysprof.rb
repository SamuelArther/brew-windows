class Sysprof < Formula
  desc "Statistical, system-wide profiler built on the Windows Performance Recorder"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

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
