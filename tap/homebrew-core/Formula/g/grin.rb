class Grin < Formula
  desc "Minimal implementation of the Mimblewimble protocol"
  homepage "https://grin.mw/"
  url "https://github.com/mimblewimble/grin/releases/download/v5.5.2/grin-v5.5.2-win-x86_64.zip"
  sha256 "7c11350effb869a6e3ed9d7e32aa2ad5ef6db4707ca659b4ac61659345d652cc"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/mimblewimble/grin/archive/refs/tags/v5.5.2.tar.gz"
  end

  def install
    bin.install "grin.exe"
  end

  test do
    system bin/"grin", "server", "config"
    assert_path_exists testpath/"grin-server.toml"
  end
end
