class IosDeploy < Formula
  desc "Detect iOS devices from the command-line (Apple Mobile Device usbmuxd)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "ios-deploy", "ios-deploy"
  end

  test do
    assert_match(/\A\S*\s?v?\d+(\.\d+)+/, shell_output("#{bin}/ios-deploy -V").strip)
    system bin/"ios-deploy", "-c", "-t", "1"
    assert_match "detect", shell_output("#{bin}/ios-deploy --help")
  end
end
