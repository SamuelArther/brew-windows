class Sandvault < Formula
  desc "Run AI agents isolated in a sandbox with its own home folder"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

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
