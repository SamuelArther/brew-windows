class Fileicon < Formula
  desc "CLI for managing custom icons for folders"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "fileicon", "fileicon"
  end

  test do
    icon = test_fixtures "test.png"
    (testpath/"folder").mkpath
    system bin/"fileicon", "set", testpath/"folder", icon
    assert_path_exists testpath/"folder/desktop.ini"
    assert_match "HAS custom icon: folder", shell_output("#{bin}/fileicon test #{testpath}/folder")
    system bin/"fileicon", "get", testpath/"folder", testpath/"out.ico"
    assert_path_exists testpath/"out.ico"
    system bin/"fileicon", "rm", testpath/"folder"
    assert_match "Has NO custom icon", shell_output("#{bin}/fileicon test #{testpath}/folder", 1)
  end
end
