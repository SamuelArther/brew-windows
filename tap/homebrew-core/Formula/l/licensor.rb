class Licensor < Formula
  desc "Write licenses to stdout"
  homepage "https://github.com/raftario/licensor"
  url "https://github.com/raftario/licensor/releases/download/v2.1.0/Windows.zip"
  sha256 "045cbd4c10205acb05c907da9bb9e02fa745b02e9ecdcad7863bada9634860bd"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/raftario/licensor/archive/refs/tags/v2.1.0.tar.gz"
  end

  def install
    bin.install "licensor.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/licensor --version 2>&1")
  end
end
