class Nift < Formula
  desc "Fast dependency-aware website generator"
  homepage "https://nift.dev/"
  url "https://github.com/nift-dev/nift/releases/download/v4.12.0/nift-4.12.0-windows-x86_64.zip"
  sha256 "0869fa351dc4d0e357bb799a21afa509a18bee1e914d068d999f19e78540c771"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/nift-dev/nift/archive/refs/tags/v4.12.0.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "nift.exe"
  end

  test do
    system bin/"nift", "init", "--ext=.html"
    assert_path_exists testpath/"public/index.html"
  end
end
