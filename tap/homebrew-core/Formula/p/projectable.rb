class Projectable < Formula
  desc "TUI file manager built for projects"
  homepage "https://dzfrias.dev/blog/projectable"
  url "https://github.com/dzfrias/projectable/releases/download/1.3.2/projectable-1.3.2-x86_64-pc-windows-gnu.zip"
  sha256 "c96f3f8896febda32934971df3faed27b59d0602c473ad4776d66b9d7b0a3486"
  license "MIT"
  revision 1

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/dzfrias/projectable/archive/refs/tags/1.3.2.tar.gz"
  end

  def install
    bin.install "prj.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/prj --version 2>&1")
  end
end
