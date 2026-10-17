class GitterCli < Formula
  desc "Extremely simple Gitter client for terminals"
  homepage "https://github.com/RodrigoEspinosa/gitter-cli"
  url "https://github.com/RodrigoEspinosa/gitter-cli/archive/refs/tags/v0.8.5.tar.gz"
  sha256 "c4e335620fc1be50569f3b7543c28ba2c6121b1c7e6d041464b29a31b3d84c25"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "197c7abcb72c24b1865e4d9932c93e7651b88930a8aa8c489bc7dd21e68b9fd8"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match "access token", pipe_output("#{bin}/gitter-cli authorize")
  end
end
