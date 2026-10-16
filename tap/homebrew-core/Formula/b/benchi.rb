class Benchi < Formula
  desc "Benchmarking tool for data pipelines"
  homepage "https://github.com/ConduitIO/benchi"
  url "https://github.com/ConduitIO/benchi/releases/download/v0.4.1/benchi_0.4.1_Windows_x86_64.tar.gz"
  sha256 "3e31e122421fcd387cd6b086b76f3eb865548e1d4c294cdd72a8f7a9ca1440bf"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/ConduitIO/benchi/archive/refs/tags/v0.4.1.tar.gz"
  end

  def install
    bin.install "benchi.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/benchi -v 2>&1")
  end
end
