class Dockerfilegraph < Formula
  desc "Visualize your multi-stage Dockerfiles"
  homepage "https://github.com/patrickhoefler/dockerfilegraph"
  url "https://github.com/patrickhoefler/dockerfilegraph/releases/download/v0.21.0/dockerfilegraph_0.21.0_windows_amd64.tar.gz"
  sha256 "051f22b9774ba4310c05caa014249b1f3f687652894dfb5dff996030f590775e"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/patrickhoefler/dockerfilegraph/archive/refs/tags/v0.21.0.tar.gz"
  end

  def install
    bin.install "dockerfilegraph.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/dockerfilegraph --version 2>&1")
  end
end
