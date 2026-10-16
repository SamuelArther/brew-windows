class Makeicns < Formula
  desc "Create icns and ico files from the command-line"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

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
