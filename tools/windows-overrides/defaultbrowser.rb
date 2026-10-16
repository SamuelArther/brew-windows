class Defaultbrowser < Formula
  desc "Command-line tool for getting & setting the default browser"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

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
