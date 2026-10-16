class Baguette < Formula
  desc "Headless iOS Simulator manager and host-side input injection for iOS 26"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "baguette", "baguette"
  end

  def caveats
    <<~EOS
      The iOS Simulator is part of Xcode and only runs on macOS. On Windows baguette lists
      device sets copied from a Mac (baguette list --device-set DIR); the commands that drive a
      live simulator explain that and exit 1.
    EOS
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/baguette --version")
    assert_match "Headless iOS simulator control", shell_output("#{bin}/baguette --help")
    assert_match "Usage: baguette", shell_output("#{bin}/baguette no-such-command 2>&1", 64)
    (testpath/"set/AB12").mkpath
    (testpath/"set/AB12/device.plist").write <<~XML
      <?xml version="1.0" encoding="UTF-8"?>
      <plist version="1.0"><dict><key>UDID</key><string>AB12</string><key>name</key><string>iPhone 17</string>
      <key>runtime</key><string>com.apple.CoreSimulator.SimRuntime.iOS-26-0</string><key>state</key><integer>3</integer></dict></plist>
    XML
    assert_match "\"runtime\":\"iOS 26.0\"", shell_output("#{bin}/baguette list --device-set #{testpath}/set")
    assert_match "only runs on macOS", shell_output("#{bin}/baguette tap 2>&1", 1)
  end
end
