class PinentryMac < Formula
  desc "Pinentry for GPG using the Windows credential prompt"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

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
