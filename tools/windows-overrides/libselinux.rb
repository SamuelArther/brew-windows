class Libselinux < Formula
  desc "Mandatory access control utilities for Windows integrity levels"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "selinuxenabled", "selinuxenabled"
    windows_exe "libexec/wintools.exe", "getenforce", "getenforce"
    windows_exe "libexec/wintools.exe", "setenforce", "setenforce"
    windows_exe "libexec/wintools.exe", "getfilecon", "getfilecon"
    windows_exe "libexec/wintools.exe", "setfilecon", "setfilecon"
    windows_exe "libexec/wintools.exe", "getpidcon", "getpidcon"
    windows_exe "libexec/wintools.exe", "matchpathcon", "matchpathcon"
  end

  def caveats
    <<~EOS
      The mandatory access control here is Windows Mandatory Integrity Control: contexts are
      integrity levels (Untrusted, Low, Medium, High, System). It is always Enforcing.
      There is no libselinux C library or SELinux policy on Windows.
    EOS
  end

  test do
    assert_match(/^(Enforcing|Permissive|Disabled)$/, shell_output(bin/"getenforce").chomp)
    system bin/"selinuxenabled"
    (testpath/"f.txt").write "x"
    assert_match "Medium", shell_output("#{bin}/getfilecon #{testpath}/f.txt")
    system bin/"setfilecon", "Low", testpath/"f.txt"
    assert_match "Low", shell_output("#{bin}/getfilecon #{testpath}/f.txt")
    shell_output("#{bin}/setenforce 0 2>&1", 1)
  end
end
