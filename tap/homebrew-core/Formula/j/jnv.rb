class Jnv < Formula
  desc "Interactive JSON filter using jq"
  homepage "https://github.com/ynqa/jnv"
  url "https://github.com/ynqa/jnv/releases/download/v0.7.1/jnv-x86_64-pc-windows-msvc.zip"
  sha256 "f76a635add18586c9473329fb37d2dca831b80d8062dccb4683f69a3bb3de5e6"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/ynqa/jnv/archive/refs/tags/v0.7.1.tar.gz"
  end

  def install
    bin.install "jnv.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/jnv --version 2>&1")
  end
end
