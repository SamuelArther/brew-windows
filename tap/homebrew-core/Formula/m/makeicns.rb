class Makeicns < Formula
  desc "Create icns and ico files from the command-line"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "makeicns", "makeicns"
  end

  test do
    system bin/"makeicns", "-in", test_fixtures("test.png"), "-out", testpath/"test.icns"
    assert_path_exists testpath/"test.icns"
    assert_equal "icns", (testpath/"test.icns").binread[0, 4]
    system bin/"makeicns", "-in", test_fixtures("test.png"), "-out", testpath/"test.ico"
    assert_path_exists testpath/"test.ico"
  end
end
