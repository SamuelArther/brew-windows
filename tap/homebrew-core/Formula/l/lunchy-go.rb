class LunchyGo < Formula
  desc "Friendly wrapper for launchd-style agents on Task Scheduler"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "lunchy", "lunchy"
  end

  test do
    ENV["HOME"] = testpath
    plist = testpath/"Library/LaunchAgents/com.example.echo.plist"
    plist.dirname.mkpath
    plist.write <<~XML
      <?xml version="1.0" encoding="UTF-8"?>
      <plist version="1.0">
      <dict>
        <key>Label</key>
        <string>com.example.echo</string>
        <key>ProgramArguments</key>
        <array>
          <string>cmd.exe</string>
          <string>/c</string>
          <string>echo hi</string>
        </array>
        <key>StartInterval</key>
        <integer>300</integer>
      </dict>
      </plist>
    XML
    assert_equal "com.example.echo\n", shell_output("#{bin}/lunchy ls echo")
    assert_match "PT300S", shell_output("#{bin}/lunchy show --task echo")
    assert_match "removed", shell_output("#{bin}/lunchy remove com.example.echo")
    refute_path_exists plist
  end
end
