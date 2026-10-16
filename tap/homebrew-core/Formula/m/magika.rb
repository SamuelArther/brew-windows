class Magika < Formula
  desc "Fast and accurate AI powered file content types detection"
  homepage "https://securityresearch.google/magika/"
  url "https://github.com/google/magika/releases/download/cli/v1.1.0/magika-cli-x86_64-pc-windows-msvc.zip"
  version "1.1.0"
  sha256 "d4de347c53e2d25f9663780c93fdf26d156284c64d6a5ec4bcdb829f9994f6f3"
  license "Apache-2.0"
  revision 1

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/google/magika/archive/refs/tags/cli/v1.1.0.tar.gz"
    regex(%r{^cli/v?(\d+(?:\.\d+)+)$}i)
  end

  def install
    bin.install "magika.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/magika --version 2>&1")
  end
end
