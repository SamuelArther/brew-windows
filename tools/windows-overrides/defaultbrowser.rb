class Defaultbrowser < Formula
  desc "Command-line tool for getting & setting the default browser"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

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
