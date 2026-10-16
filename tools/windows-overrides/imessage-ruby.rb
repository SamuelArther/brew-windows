class ImessageRuby < Formula
  desc "Command-line tool to send text through the phone paired with Phone Link"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

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
