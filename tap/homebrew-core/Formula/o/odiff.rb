class Odiff < Formula
  desc "Very fast SIMD-first image comparison library (with nodejs API)"
  homepage "https://github.com/dmtrKovalenko/odiff"
  url "https://github.com/dmtrKovalenko/odiff/releases/download/v4.5.0/odiff-windows-x64.exe"
  sha256 "d2d38dfbdeff70c6d6a2f48ea41c9fe702f866c1f836c6896a677bf97652e4f1"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/dmtrKovalenko/odiff/archive/refs/tags/v4.5.0.tar.gz"
  end

  def install
    bin.install "odiff-windows-x64.exe" => "odiff.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/odiff --version 2>&1")
  end
end
