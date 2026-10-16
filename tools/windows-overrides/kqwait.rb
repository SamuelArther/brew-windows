class Kqwait < Formula
  desc "Wait for events on files or directories"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "kqwait", "kqwait"
  end

  test do
    system bin/"kqwait", "-v"
    (testpath/"watched").write "x"
    output = IO.popen("#{bin}/kqwait #{testpath}/watched") do |io|
      sleep 2
      (testpath/"watched").append_lines "changed"
      io.read
    end
    assert_match "watched", output
  end
end
