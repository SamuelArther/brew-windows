class Sbjson < Formula
  desc "JSON CLI parser & reformatter in the style of SBJson v5"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

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
