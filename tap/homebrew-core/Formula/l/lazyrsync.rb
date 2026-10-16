class Lazyrsync < Formula
  desc "Terminal UI for rsync, written in Rust"
  homepage "https://lazyrsync.westpoint.io/"
  url "https://github.com/westpoint-io/lazyrsync/archive/refs/tags/v0.3.0.tar.gz"
  sha256 "7ddb32c20688c1e56032207a5ed8edd5db09f9aaaad43741de13bef715bc1a4d"
  license "MIT"
  head "https://github.com/westpoint-io/lazyrsync.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "f8c19f923c00eea7a436edce6f3659cc092a3e438a2626b464e1bd5aa5581f7c"
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
