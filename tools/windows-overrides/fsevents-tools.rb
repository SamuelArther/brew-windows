class FseventsTools < Formula
  desc "Command-line utilities for file system change notifications"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

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
