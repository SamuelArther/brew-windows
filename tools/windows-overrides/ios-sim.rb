class IosSim < Formula
  desc "Command-line application launcher for the iOS Simulator"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "ios-sim", "ios-sim"
  end

  def caveats
    <<~EOS
      The iOS Simulator is part of Xcode and only runs on macOS: ios-sim checks app bundles
      and reports that no simulator runtimes exist on Windows.
    EOS
  end

  test do
    system bin/"ios-sim", "--help"
    assert_match "showdevicetypes", shell_output("#{bin}/ios-sim --help")
    assert_match "only runs on macOS", shell_output("#{bin}/ios-sim showsdks 2>&1")
    (testpath/"Demo.app").mkpath
    (testpath/"Demo.app/Info.plist").write <<~XML
      <?xml version="1.0" encoding="UTF-8"?>
      <plist version="1.0"><dict><key>CFBundleIdentifier</key><string>com.example.demo</string>
      <key>CFBundleSupportedPlatforms</key><array><string>iPhoneSimulator</string></array></dict></plist>
    XML
    assert_match "com.example.demo", shell_output("#{bin}/ios-sim launch #{testpath}/Demo.app 2>&1", 1)
    assert_match "unknown command", shell_output("#{bin}/ios-sim frobnicate 2>&1", 2)
  end
end
