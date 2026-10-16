class PamReattach < Formula
  desc "Run programs in the user's interactive logon session (pam_reattach's CLI)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

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
