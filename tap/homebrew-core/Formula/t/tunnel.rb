class Tunnel < Formula
  desc "Expose local servers to the internet securely"
  homepage "https://github.com/labstack/tunnel-client"
  url "https://github.com/labstack/tunnel-client/archive/refs/tags/v0.5.15.tar.gz"
  sha256 "7a57451416b76dbf220e69c7dd3e4c33dc84758a41cdb9337a464338565e3e6e"
  license "MIT"

  # `https://tunnel.labstack.com/docs` is no longer accessible
  deprecate! date: "2025-02-23", because: :unmaintained
  disable! date: "2026-02-23", because: :unmaintained

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args, "./cmd/tunnel"
  end

  test do
    assert_match "you need an api key", shell_output("#{bin}/tunnel 8080", 1)
  end
end
