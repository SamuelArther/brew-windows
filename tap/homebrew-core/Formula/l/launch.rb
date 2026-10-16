class Launch < Formula
  desc "Command-line launcher for Windows, in the spirit of `open`"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

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
