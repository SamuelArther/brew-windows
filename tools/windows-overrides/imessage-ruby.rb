class ImessageRuby < Formula
  desc "Command-line tool to send text through the phone paired with Phone Link"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "imessage", "imessage"
  end

  def caveats
    <<~EOS
      Messages go out from the phone paired with Phone Link: each one opens in Phone Link
      ready to send. Pair your phone in Phone Link first.
    EOS
  end

  test do
    assert_match "imessage v#{version}", shell_output("#{bin}/imessage --version")
    assert_match "at least one contact", shell_output("#{bin}/imessage -t hello 2>&1", 2)
    assert_match "attachments", shell_output("#{bin}/imessage -t hi -c +15550100 -a x.png 2>&1", 1)
  end
end
