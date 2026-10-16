class Fileicon < Formula
  desc "CLI for managing custom icons for folders"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

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
