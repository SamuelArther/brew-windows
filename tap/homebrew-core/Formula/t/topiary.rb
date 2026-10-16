class Topiary < Formula
  desc "Uniform formatter for simple languages, as part of the Tree-sitter ecosystem"
  homepage "https://topiary.tweag.io/"
  url "https://github.com/topiary/topiary/releases/download/v0.8.0/topiary-cli-x86_64-pc-windows-msvc.zip"
  sha256 "fd2411037a0cc85715225e5968038db067f3833965041cbcf1babedfef7fd8e1"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/topiary/topiary/archive/refs/tags/v0.8.0.tar.gz"
  end

  def install
    bin.install "topiary.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/topiary --version 2>&1")
  end
end
