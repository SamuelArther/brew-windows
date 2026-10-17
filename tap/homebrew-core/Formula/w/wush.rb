class Wush < Formula
  desc "Transfer files between computers via WireGuard"
  homepage "https://github.com/coder/wush"
  url "https://github.com/coder/wush/archive/refs/tags/v0.4.1.tar.gz"
  sha256 "77d5a912465d1e8ec478252a9a69a04d39af75a126ac9ed94823f33a60b3d8f9"
  license "CC0-1.0"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "e20b28ba6bf6250882420fb1d98c143ed6172d6d31cbe060f01aa62b8d6cd736"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: "-X main.version=#{version}"), "./cmd/wush"
  end

  test do
    read, write = IO.pipe

    pid = fork do
      exec bin/"wush", "serve", out: write, err: write
    end

    output = read.gets
    assert_includes output, "Picked DERP region"
  ensure
    Process.kill "TERM", pid
    Process.wait pid
  end
end
