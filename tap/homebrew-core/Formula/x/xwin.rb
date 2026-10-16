class Xwin < Formula
  desc "Microsoft CRT and Windows SDK headers and libraries loader"
  homepage "https://github.com/Jake-Shadle/xwin"
  url "https://github.com/Jake-Shadle/xwin/releases/download/0.10.0/xwin-0.10.0-x86_64-pc-windows-msvc.tar.gz"
  sha256 "96e83665ef5c1406aabd0be603a8d08e67e604f36fda630b6807ee948a614e26"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Jake-Shadle/xwin/archive/refs/tags/0.10.0.tar.gz"
  end

  def install
    bin.install "xwin.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/xwin --version 2>&1")
  end
end
