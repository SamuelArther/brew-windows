class Flvmeta < Formula
  desc "Manipulate Adobe flash video files (FLV)"
  homepage "https://flvmeta.com/"
  url "https://github.com/noirotm/flvmeta/releases/download/v1.3.0/flvmeta-1.3.0-win64.zip"
  sha256 "59291b72efa297360f9c7cd01cad318ecd646e78811a66a2b0373071df34d331"
  license "GPL-2.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/noirotm/flvmeta/archive/refs/tags/v1.3.0.tar.gz"
  end

  def install
    bin.install "flvmeta.exe"
  end

  test do
    system bin/"flvmeta", "-V"
  end
end
