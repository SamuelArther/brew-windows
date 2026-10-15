class Ncompress < Formula
  desc "Fast, simple LZW file compressor"
  homepage "https://vapier.github.io/ncompress/"
  url "https://github.com/vapier/ncompress/releases/download/v5.0/compress-windows-x64.exe"
  sha256 "f0760505b513fe9f4524f86d7e5b9bbc990bd95e2df96d8337acf6f2c5d7ca62"
  license "Unlicense"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/vapier/ncompress/archive/refs/tags/v5.0.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "compress-windows-x64.exe" => "compress.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/compress -V 2>&1")
  end
end
