class Zzz < Formula
  desc "Command-line tool to put the PC to sleep"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "Zzz", "Zzz"
  end

  test do
    assert_match "Puts the PC to sleep", shell_output("#{bin}/Zzz -h")
    assert_match "unknown option", shell_output("#{bin}/Zzz --bogus 2>&1", 2)
  end
end
