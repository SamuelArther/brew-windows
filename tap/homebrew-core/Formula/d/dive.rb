class Dive < Formula
  desc "Tool for exploring each layer in a docker image"
  homepage "https://github.com/wagoodman/dive"
  url "https://github.com/wagoodman/dive/releases/download/v0.13.1/dive_0.13.1_windows_amd64.zip"
  sha256 "3e764ff28c7b89f4da679deac80483249fbbae3a1d512c103d54609eec09086a"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "dive.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/dive --version")
  end
end
