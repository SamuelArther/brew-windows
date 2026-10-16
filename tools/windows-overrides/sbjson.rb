class Sbjson < Formula
  desc "JSON CLI parser & reformatter in the style of SBJson v5"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

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
