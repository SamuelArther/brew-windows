class Osxutils < Formula
  desc "Collection of command-line file and system utilities"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "osxutils", "osxutils"
    windows_exe "libexec/wintools.exe", "cpath", "cpath"
    windows_exe "libexec/wintools.exe", "finfo", "finfo"
    windows_exe "libexec/wintools.exe", "lsmac", "lsmac"
    windows_exe "libexec/wintools.exe", "rcmac", "rcmac"
    windows_exe "libexec/wintools.exe", "mkalias", "mkalias"
    windows_exe "libexec/wintools.exe", "setsuffix", "setsuffix"
    windows_exe "libexec/wintools.exe", "getfcomment", "getfcomment"
    windows_exe "libexec/wintools.exe", "setfcomment", "setfcomment"
    windows_exe "libexec/wintools.exe", "google", "google"
    windows_exe "libexec/wintools.exe", "wikipedia", "wikipedia"
    windows_exe "libexec/wintools.exe", "wsupdate", "wsupdate"
  end

  test do
    assert_match "osxutils", shell_output("#{bin}/osxutils")
    (testpath/"a.txt").write "hi"
    assert_match "Modified", shell_output("#{bin}/lsmac #{testpath}")
    assert_match "Kind:", shell_output("#{bin}/finfo #{testpath}/a.txt")
    system bin/"setfcomment", testpath/"a.txt", "note"
    assert_match "note", shell_output("#{bin}/getfcomment #{testpath}/a.txt")
    system bin/"mkalias", testpath/"a.txt", testpath/"a.lnk"
    assert_path_exists testpath/"a.lnk"
  end
end
