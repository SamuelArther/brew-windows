class Timedog < Formula
  desc "Lists files that were saved by a File History backup"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "timedog", "timedog"
  end

  test do
    assert_match "No machine directory found for host", shell_output("#{bin}/timedog 2>&1", 1)
    docs = testpath/"fh/Data/C/Users/me"
    docs.mkpath
    (docs/"a (2026_10_15 08_00_00 UTC).txt").write "aaaa"
    (docs/"a (2026_10_16 09_30_00 UTC).txt").write "aaaaaaaa"
    output = shell_output("#{bin}/timedog #{testpath}/fh")
    assert_match "Comparing FH backup 2026-10-16-093000 to 2026-10-15-080000", output
    assert_match "4B->       8B    C:\\Users\\me\\a.txt", output
    assert_match "Total Backup: 1 changed file, 8B", output
  end
end
