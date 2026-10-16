class WifiPassword < Formula
  desc "Show the current Wi-Fi network password"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "wifi-password", "wifi-password"
  end

  def caveats
    <<~EOS
      Reading the key of a network saved for all users needs an elevated (Administrator) prompt.
    EOS
  end

  test do
    assert_match "wifi-password", shell_output("#{bin}/wifi-password --version")
    assert_match "Wi-Fi", shell_output("#{bin}/wifi-password 2>&1", 1)
  end
end
