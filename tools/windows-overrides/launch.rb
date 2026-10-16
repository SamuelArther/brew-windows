class Launch < Formula
  desc "Command-line launcher for Windows, in the spirit of `open`"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "launch", "launch"
  end

  test do
    touch testpath/"doc.txt"
    assert_match "doc.txt", shell_output("#{bin}/launch -n #{testpath}/doc.txt")
    assert_match "document", shell_output("#{bin}/launch -f #{testpath}/doc.txt")
    assert_match "creator codes", shell_output("#{bin}/launch -c ttxt #{testpath}/doc.txt 2>&1", 1)
  end
end
