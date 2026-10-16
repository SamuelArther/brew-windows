class Acl < Formula
  desc "Commands for manipulating NTFS access control lists as POSIX ACLs"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "getfacl", "getfacl"
    windows_exe "libexec/wintools.exe", "setfacl", "setfacl"
    windows_exe "libexec/wintools.exe", "chacl", "chacl"
  end

  test do
    assert_match "getfacl", shell_output("#{bin}/getfacl --version")
    touch testpath/"test.txt"
    system bin/"chacl", "u::rw-,g::r-x,o::r--", "test.txt"
    output = shell_output("#{bin}/getfacl --omit-header test.txt")
    assert_match "user::rw-", output
    assert_match "other::r--", output
    system bin/"setfacl", "--modify=g:Users:x", "test.txt"
    output = shell_output("#{bin}/getfacl --omit-header test.txt")
    assert_match "group:Users:--x", output
    assert_match "mask::", output
    assert_match "test.txt [u::rw-", shell_output("#{bin}/chacl -l test.txt")
  end
end
