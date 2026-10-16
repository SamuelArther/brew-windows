class Cliclick < Formula
  desc "Tool for emulating mouse and keyboard events"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "cliclick", "cliclick"
  end

  test do
    system bin/"cliclick", "p:OK"
    # test mode only prints what it would do
    output = shell_output("#{bin}/cliclick -m test c:10,20 kp:return t:hi")
    assert_match "Click at 10,20", output
    assert_match "Press return key", output
  end
end
