class Triangle < Formula
  desc "Convert images to computer generated art using Delaunay triangulation"
  homepage "https://github.com/esimov/triangle"
  url "https://github.com/esimov/triangle/releases/download/v2.0.0/triangle-2.0.0-windows-amd64.zip"
  sha256 "baffcabb7acbfb3e1f05557528868e9b399d798cd103cec0fe46a398af54a056"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/esimov/triangle/archive/refs/tags/v2.0.0.tar.gz"
  end

  def install
    bin.install "triangle.exe"
  end

  test do
    system bin/"triangle", "--help"
  end
end
