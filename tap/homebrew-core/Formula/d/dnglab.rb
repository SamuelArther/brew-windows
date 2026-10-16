class Dnglab < Formula
  desc "Camera RAW to DNG file format converter"
  homepage "https://github.com/dnglab/dnglab"
  url "https://github.com/dnglab/dnglab/releases/download/v0.8.0/dnglab-win-x64_v0.8.0.zip"
  sha256 "d3add6b71568bf5f7938468b0b40a0ee1eaec01e0057fe78dcc453188010be9a"
  license "LGPL-2.1-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/dnglab/dnglab/archive/refs/tags/v0.8.0.tar.gz"
  end

  def install
    bin.install "dnglab.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/dnglab --version")

    touch testpath/"not_a_dng.dng"
    output = shell_output("#{bin}/dnglab analyze --raw-checksum not_a_dng.dng 2>&1", 7)
    assert_match "Error: No decoder found, model '', make: '', mode: ''", output
  end
end
