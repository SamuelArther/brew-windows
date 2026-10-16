class Kqwait < Formula
  desc "Wait for events on files or directories"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

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
