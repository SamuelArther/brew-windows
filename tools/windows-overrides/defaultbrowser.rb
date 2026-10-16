class Defaultbrowser < Formula
  desc "Command-line tool for getting & setting the default browser"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "defaultbrowser", "defaultbrowser"
  end

  test do
    # Microsoft Edge is registered as a browser on every Windows install
    assert_match "msedge", shell_output(bin/"defaultbrowser")
    assert_match "not available", shell_output("#{bin}/defaultbrowser nosuchbrowser 2>&1", 1)
  end
end
