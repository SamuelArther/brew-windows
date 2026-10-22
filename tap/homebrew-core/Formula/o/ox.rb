class Ox < Formula
  desc "Independent Rust text editor that runs in your terminal"
  homepage "https://github.com/curlpipe/ox"
  url "https://github.com/curlpipe/ox/archive/refs/tags/0.7.7.tar.gz"
  sha256 "9dc869836e92beb5806739da25bf929b45bbb4173a6bffac31b762fcece74a0a"
  license "GPL-2.0-only"
  head "https://github.com/curlpipe/ox.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "cb4571c65ecc79cf9aeab401e76e4aef5a1bf62c544712f9ea47292f00ed20d5"
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
    # ox is a TUI application, hard to test in CI
    # see https://github.com/curlpipe/ox/issues/178 for discussions
    assert_match version.to_s, shell_output("#{bin}/ox --version")
  end
end
