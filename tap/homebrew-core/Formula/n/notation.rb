class Notation < Formula
  desc "CLI tool to sign and verify OCI artifacts and container images"
  homepage "https://notaryproject.dev/"
  url "https://github.com/notaryproject/notation/releases/download/v1.3.2/notation_1.3.2_windows_amd64.zip"
  sha256 "014f25a530eee17520c8e1eb7380e4bd02ff6fc04479a07a890954e3b7ddfdc7"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/notaryproject/notation/archive/refs/tags/v1.3.2.tar.gz"
  end

  def install
    bin.install "notation.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/notation version 2>&1")
  end
end
