class FseventsTools < Formula
  desc "Command-line utilities for file system change notifications"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "notifywait", "notifywait"
    windows_exe "libexec/wintools.exe", "notifyloop", "notifyloop"
  end

  test do
    (testpath/"testfile").write "x"
    output = IO.popen("#{bin}/notifywait #{testpath}/testfile") do |io|
      sleep 2
      (testpath/"testfile").append_lines "changed"
      io.read
    end
    assert_match "Watching", output
    assert_match "testfile", output
    assert_match "Usage: notifyloop", shell_output("#{bin}/notifyloop --help")
  end
end
