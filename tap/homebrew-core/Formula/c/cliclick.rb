class Cliclick < Formula
  desc "Tool for emulating mouse and keyboard events"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

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
