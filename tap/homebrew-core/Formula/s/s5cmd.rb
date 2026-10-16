class S5cmd < Formula
  desc "Parallel S3 and local filesystem execution tool"
  homepage "https://github.com/peak/s5cmd/"
  url "https://github.com/peak/s5cmd/releases/download/v2.3.0/s5cmd_2.3.0_Windows-64bit.zip"
  sha256 "9d8a98c1d9f6284c6d54aeec3daeb5f1412fd101850ae756c1e70dd8ffe52166"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/peak/s5cmd/archive/refs/tags/v2.3.0.tar.gz"
  end

  def install
    bin.install "s5cmd.exe"
  end

  test do
    assert_match "no valid providers in chain", shell_output("#{bin}/s5cmd --retry-count 0 ls s3://brewtest 2>&1", 1)
    assert_match version.to_s, shell_output("#{bin}/s5cmd version")
  end
end
