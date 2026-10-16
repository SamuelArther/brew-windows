class Ox < Formula
  desc "Independent Rust text editor that runs in your terminal"
  homepage "https://github.com/curlpipe/ox"
  url "https://github.com/curlpipe/ox/archive/refs/tags/0.7.7.tar.gz"
  sha256 "9dc869836e92beb5806739da25bf929b45bbb4173a6bffac31b762fcece74a0a"
  license "GPL-2.0-only"
  head "https://github.com/curlpipe/ox.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "a749a9756ac748bb617a460a30d7a12c253c27faa33b82ef12232dc350fea16a"
  end

  depends_on "rust" => :build

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    # ox is a TUI application, hard to test in CI
    # see https://github.com/curlpipe/ox/issues/178 for discussions
    assert_match version.to_s, shell_output("#{bin}/ox --version")
  end
end
