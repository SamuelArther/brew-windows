class Zsign < Formula
  desc "Cross-platform codesigning tool for iOS apps"
  homepage "https://github.com/zhlynn/zsign"
  url "https://github.com/zhlynn/zsign/releases/download/v1.1.2/zsign-windows-x64.zip"
  sha256 "96b5bf7029a52c67cdb78b68b3666d8ff1e31e5c435535964be2a482afc09e5c"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/zhlynn/zsign/archive/refs/tags/v1.1.2.tar.gz"
  end

  def install
    bin.install "zsign.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/zsign --version 2>&1")
  end
end
