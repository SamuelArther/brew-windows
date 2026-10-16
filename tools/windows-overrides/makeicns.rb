class Makeicns < Formula
  desc "Create icns and ico files from the command-line"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

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
