class Imgdiff < Formula
  desc "Pixel-by-pixel image difference tool"
  homepage "https://github.com/n7olkachev/imgdiff"
  url "https://github.com/n7olkachev/imgdiff/releases/download/v1.0.2/imgdiff-windows-amd64.exe"
  sha256 "7d24049c573218a98879520a1b5ee39120f3147d7c1580ec848459b1e6eed1fd"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/n7olkachev/imgdiff/archive/refs/tags/v1.0.2.tar.gz"
  end

  def install
    bin.install "imgdiff-windows-amd64.exe" => "imgdiff.exe"
  end

  test do
    system bin/"imgdiff", "--version"
  end
end
