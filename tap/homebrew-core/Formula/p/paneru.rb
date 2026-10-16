# paneru is a macOS-only sliding (niri-style) tiling window manager. On Windows the standard tiling
# window manager is komorebi, which has the same scrolling layout; this installs komorebi's official
# release and runs it as `paneru`. komorebic (its control CLI) and komorebi-bar come along.
class Paneru < Formula
  desc "Sliding, tiling window manager (komorebi on Windows)"
  homepage "https://github.com/LGUG2Z/komorebi"
  url "https://github.com/LGUG2Z/komorebi/releases/download/v0.1.41/komorebi-0.1.41-x86_64-pc-windows-msvc.zip"
  sha256 "838b1cbe551dbfada306a91e23fd536f8cea059ff619ad98495379e5228055b9"
  license :cannot_represent

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/komorebi.exe", "paneru"
    windows_exe "libexec/komorebic.exe", "komorebic"
    windows_exe "libexec/komorebi-bar.exe", "komorebi-bar"
  end

  def caveats
    <<~EOS
      paneru runs komorebi, the Windows tiling window manager. To get paneru's sliding layout:
        komorebic quickstart
        komorebic start
        komorebic change-layout scrolling
      komorebi is free for personal use; see its license for other uses:
        https://github.com/LGUG2Z/komorebi/blob/master/LICENSE.md
    EOS
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/komorebic --version")
    assert_match version.to_s, shell_output("#{bin}/paneru --version")
  end
end
