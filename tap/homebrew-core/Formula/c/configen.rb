class Configen < Formula
  desc "Configuration file code generator for use in Xcode projects"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "configen", "configen"
  end

  test do
    (testpath/"test.plist").write <<~XML
      <?xml version="1.0" encoding="UTF-8"?>
      <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
      <plist version="1.0">
      <dict>
        <key>testURL</key>
        <string>https://example.com/api</string>
        <key>retryCount</key>
        <integer>2</integer>
      </dict>
      </plist>
    XML
    (testpath/"test.map").write "testURL : URL\nretryCount : Int\n"
    system bin/"configen", "-p", "test.plist", "-h", "test.map", "-n", "AppConfig", "-o", testpath
    assert_path_exists testpath/"AppConfig.swift"
    assert_match "static let testURL: URL = URL(string: \"https://example.com/api\")", (testpath/"AppConfig.swift").read
    assert_match "static let retryCount: Int = 2", (testpath/"AppConfig.swift").read
  end
end
