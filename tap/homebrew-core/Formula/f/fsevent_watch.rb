class FseventWatch < Formula
  desc "File system events client with FSEvents-style output"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "fsevent_watch", "fsevent_watch"
  end

  test do
    system bin/"fsevent_watch", "--version"
    assert_match "--file-events", shell_output("#{bin}/fsevent_watch --help")
    assert_match "unknown format", shell_output("#{bin}/fsevent_watch --format=bogus 2>&1", 2)
  end
end
