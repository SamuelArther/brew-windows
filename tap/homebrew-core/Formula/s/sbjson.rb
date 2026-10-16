class Sbjson < Formula
  desc "JSON CLI parser & reformatter in the style of SBJson v5"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "sbjson", "sbjson"
  end

  test do
    (testpath/"in.json").write "[true,false,\"string\",42.001e3,[],{}]\n"
    assert_equal "true\nfalse\n\"string\"\n42001\n[]\n{}\n", shell_output("#{bin}/sbjson --unwrap-root in.json")
  end
end
