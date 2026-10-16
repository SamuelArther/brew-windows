class Fmdiff < Formula
  desc "Use WinMerge as a diff command for Subversion, Mercurial and git"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "fmdiff", "fmdiff"
    windows_exe "libexec/wintools.exe", "fmdiff3", "fmdiff3"
    windows_exe "libexec/wintools.exe", "fmresolve", "fmresolve"
    windows_exe "libexec/wintools.exe", "fmmerge", "fmmerge"
  end

  test do
    ENV.prepend_path "PATH", testpath
    # dummy filemerge script
    (testpath/"filemerge.bat").write "@echo it works\r\n"
    touch testpath/"test"
    assert_match "it works", shell_output("#{bin}/fmdiff #{testpath}/test #{testpath}/test")
  end
end
