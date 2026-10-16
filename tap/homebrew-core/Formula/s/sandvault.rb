class Sandvault < Formula
  desc "Run AI agents isolated in a sandbox with its own home folder"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "sv", "sv"
    windows_exe "libexec/wintools.exe", "sandvault", "sandvault"
  end

  def caveats
    <<~EOS
      sv runs agents in a Windows AppContainer: they see only the current directory and
      %LOCALAPPDATA%\sandvault\home, which becomes their home folder.
    EOS
  end

  test do
    assert_equal "sv version #{version}", shell_output("#{bin}/sv --version").chomp
    assert_match "sandvault", shell_output("#{bin}/sv run cmd /c echo %USERPROFILE%")
    system bin/"sv", "uninstall"
  end
end
