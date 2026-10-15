class Ripgrep < Formula
  desc "Search tool like grep and The Silver Searcher"
  homepage "https://github.com/BurntSushi/ripgrep"
  version "15.2.0"
  url "https://github.com/BurntSushi/ripgrep/releases/download/15.2.0/ripgrep-15.2.0-x86_64-pc-windows-msvc.zip"
  sha256 "71b2fef860abe467217a538ff31de02f5258807c0129f771846f87bd029aafc5"
  license "Unlicense"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/rg.exe", "rg"
  end

  test do
    (testpath/"Hello.txt").write("Hello World!")
    system bin/"rg", "Hello World!", testpath
  end
end
