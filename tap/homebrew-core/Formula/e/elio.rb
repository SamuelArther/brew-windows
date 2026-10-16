class Elio < Formula
  desc "Batteries-included terminal file manager with rich previews"
  homepage "https://elio-fm.github.io/"
  url "https://github.com/elio-fm/elio/releases/download/v1.13.0/elio-1.13.0-x86_64-pc-windows-msvc.zip"
  sha256 "38762a71ba9f5b294f0981adf9962edbfe5409a4bfe93ecf3013a81ce1e323d3"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/elio-fm/elio/archive/refs/tags/v1.13.0.tar.gz"
  end

  def install
    bin.install "elio.exe"
  end

  test do
    missing = testpath/"missing-directory"
    output = shell_output("#{bin}/elio #{missing} 2>&1", 1)
    assert_match "no such file or directory", output
  end
end
