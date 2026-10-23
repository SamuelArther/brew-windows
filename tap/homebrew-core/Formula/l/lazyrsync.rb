class Lazyrsync < Formula
  desc "Terminal UI for rsync, written in Rust"
  homepage "https://lazyrsync.westpoint.io/"
  url "https://github.com/westpoint-io/lazyrsync/archive/refs/tags/v0.4.0.tar.gz"
  sha256 "e8995ca91c75dc67d88ad4e09f74a2f7863387262b63ee299833a4f9d4c7de3c"
  license "MIT"
  head "https://github.com/westpoint-io/lazyrsync.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "77cd4aba78e7082d1c53ece1b4a9078447c6f2ce480fc2a57676172a719291e8"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/lazyrsync --version")

    assert_match "No profiles", shell_output("#{bin}/lazyrsync list")
  end
end
