class Sbjson < Formula
  desc "JSON CLI parser & reformatter in the style of SBJson v5"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

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
