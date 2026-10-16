class Kqwait < Formula
  desc "Wait for events on files or directories"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

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
