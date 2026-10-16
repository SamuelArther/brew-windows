class WifiPassword < Formula
  desc "Show the current Wi-Fi network password"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

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
