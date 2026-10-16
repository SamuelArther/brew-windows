class FseventsTools < Formula
  desc "Command-line utilities for file system change notifications"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

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
