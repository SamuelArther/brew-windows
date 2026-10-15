class MsdfAtlasGen < Formula
  desc "Generator of multi-channel signed distance field atlases from fonts"
  homepage "https://github.com/Chlumsky/msdf-atlas-gen"
  url "https://github.com/Chlumsky/msdf-atlas-gen/releases/download/v1.4/msdf-atlas-gen-1.4-win64.zip"
  sha256 "55ab2bef57b9c7bf305b610989fa8eeda812d85bc93e632b8c39ce05b6e64e9d"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Chlumsky/msdf-atlas-gen/archive/refs/tags/v1.4.tar.gz"
  end

  def install
    bin.install "msdf-atlas-gen.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/msdf-atlas-gen --version 2>&1")
  end
end
