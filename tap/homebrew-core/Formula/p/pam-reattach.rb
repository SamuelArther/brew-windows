class PamReattach < Formula
  desc "Run programs in the user's interactive logon session (pam_reattach's CLI)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "reattach-to-session-namespace", "reattach-to-session-namespace"
  end

  def caveats
    <<~EOS
      Windows has no PAM, so the pam_reattach.so module itself has no counterpart;
      reattach-to-session-namespace runs a program in your interactive logon session
      (for example from an SSH login, to reach the clipboard or Windows Hello).
    EOS
  end

  test do
    assert_match "Windows", shell_output("#{bin}/reattach-to-session-namespace cmd /c ver")
    assert_match "Usage:", shell_output("#{bin}/reattach-to-session-namespace 2>&1", 1)
  end
end
