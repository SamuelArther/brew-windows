class Localtunnel < Formula
  desc "Exposes your localhost to the world for easy testing and sharing"
  homepage "https://theboroer.github.io/localtunnel-www/"
  url "https://registry.npmjs.org/localtunnel/-/localtunnel-2.0.2.tgz"
  sha256 "6fb76f0ac6b92989669a5b7bb519361e07301965ea1f5a04d813ed59ab2e0c34"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "99b19e58ea117079f3d809b1cc127eb3f29055402740226ec6bf89d0deb63d8c"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    # Suppress node warning during runtime
    ENV["NODE_NO_WARNINGS"] = "1"

    require "pty"
    port = free_port

    stdout, _stdin, _pid = PTY.spawn("#{bin}/lt --port #{port}")
    assert_match "your url is:", stdout.readline

    assert_match version.to_s, shell_output("#{bin}/lt --version")
  end
end
