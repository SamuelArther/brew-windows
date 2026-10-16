class IosDeploy < Formula
  desc "Detect iOS devices from the command-line (Apple Mobile Device usbmuxd)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "ios-deploy", "ios-deploy"
  end

  test do
    assert_equal version.to_s, shell_output("#{bin}/ios-deploy -V").strip
    system bin/"ios-deploy", "-c", "-t", "1"
    assert_match "detect", shell_output("#{bin}/ios-deploy --help")
  end
end
