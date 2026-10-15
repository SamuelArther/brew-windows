# 0.10.10 has no Windows build upstream; 0.10.9 is the newest release with an official x86_64 Windows binary.
# rga searches through ripgrep's rg.exe, which must be on PATH.
class RipgrepAll < Formula
  desc "Wrapper around ripgrep that adds multiple rich file types"
  homepage "https://github.com/phiresky/ripgrep-all"
  url "https://github.com/phiresky/ripgrep-all/releases/download/v0.10.9/ripgrep_all-v0.10.9-x86_64-pc-windows-msvc.zip"
  sha256 "da03e969ac01839443739c547ca8eb521e648e712251d61a98d34a1521b9f43b"
  license "AGPL-3.0-or-later"

  livecheck do
    url :stable
    strategy :github_latest
  end

  depends_on "ripgrep"

  def install
    install_tree
    windows_exe "libexec/rga.exe", "rga"
    windows_exe "libexec/rga-preproc.exe", "rga-preproc"
    windows_exe "libexec/rga-fzf.exe", "rga-fzf"
    windows_exe "libexec/rga-fzf-open.exe", "rga-fzf-open"
  end

  test do
    (testpath/"file.txt").write("Hello World")
    assert_match "Hello World", shell_output("#{bin}/rga Hello #{testpath}")
  end
end
