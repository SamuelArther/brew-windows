class Attr < Formula
  desc "Manipulate extended attributes (NTFS alternate data streams)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "attr", "attr"
    windows_exe "libexec/wintools.exe", "getfattr", "getfattr"
    windows_exe "libexec/wintools.exe", "setfattr", "setfattr"
  end

  test do
    (testpath/"test.txt").write("Hello World!\n")
    output = pipe_output("#{bin}/attr -s name test.txt", "", 0)
    assert_match 'Attribute "name" set to a 0 byte value for test.txt', output
    output = shell_output("#{bin}/attr -l test.txt")
    assert_match 'Attribute "name" has a 0 byte value for test.txt', output
    system bin/"setfattr", "-n", "user.comment", "-v", "hello", "test.txt"
    assert_match 'user.comment="hello"', shell_output("#{bin}/getfattr -d test.txt")
    assert_equal "Hello World!\n", (testpath/"test.txt").read
  end
end
