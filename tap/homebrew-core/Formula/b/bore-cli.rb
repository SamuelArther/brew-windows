class BoreCli < Formula
  desc "Modern, simple TCP tunnel in Rust that exposes local ports to a remote server"
  homepage "https://github.com/ekzhang/bore"
  url "https://github.com/ekzhang/bore/archive/refs/tags/v0.6.0.tar.gz"
  sha256 "ab3175a6f304c7efdcacd0f6a0e4950f49eb31cb2a3ae9b4928c97ed8d03861c"
  license "MIT"
  head "https://github.com/ekzhang/bore.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "d36c05ec1ecdb3c8aa3fe48f5eb9080341d01a7083bef9eaa2546902efbed6a7"
  end

  depends_on "rust" => :build

  allow_network_access! :test

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    _, stdout, wait_thr = Open3.popen2("#{bin}/bore server")
    assert_match "server listening", stdout.gets("\n")

    assert_match version.to_s, shell_output("#{bin}/bore --version")
  ensure
    Process.kill("TERM", wait_thr.pid)
  end
end
