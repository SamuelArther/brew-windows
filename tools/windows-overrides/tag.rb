class Tag < Formula
  desc "Manipulate and query Finder-style tags on files (stored in NTFS streams)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "tag", "tag"
  end

  test do
    test_tag = "test_tag"
    test_file = testpath/"test_file"
    touch test_file
    system bin/"tag", "--add", test_tag, test_file
    assert_equal test_tag, shell_output("#{bin}/tag --list --no-name #{test_file}").chomp
    system bin/"tag", "--add", "Red,work", test_file
    assert_match "test_file", shell_output("#{bin}/tag --match work --enter #{testpath}")
    system bin/"tag", "--remove", "*", test_file
    assert_empty shell_output("#{bin}/tag --list --no-name #{test_file}").strip
    assert_match "tag v", shell_output("#{bin}/tag --version")
  end
end
