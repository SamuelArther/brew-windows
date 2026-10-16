class PinentryMac < Formula
  desc "Pinentry for GPG using the Windows credential prompt"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "pinentry-mac", "pinentry-mac"
  end

  def caveats
    <<~EOS
      To use it with GnuPG, add this line to gpg-agent.conf and restart gpg-agent:
        pinentry-program <HOMEBREW_PREFIX>/bin/pinentry-mac
    EOS
  end

  test do
    assert_match "pinentry", shell_output("#{bin}/pinentry-mac --version")
    output = pipe_output("#{bin}/pinentry-mac", "GETINFO version\nGETINFO flavor\nBYE\n", 0)
    assert_match "D 1.3.1", output
    assert_match "OK closing connection", output
  end
end
