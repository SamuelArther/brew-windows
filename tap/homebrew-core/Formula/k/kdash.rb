class Kdash < Formula
  desc "Simple and fast dashboard for Kubernetes"
  homepage "https://kdash-rs.github.io/"
  url "https://github.com/kdash-rs/kdash/releases/download/v2.1.1/kdash-windows.tar.gz"
  sha256 "0a67aa5658c10117918b5782a4563d2f1ed9466165d889e1541a31461abb1291"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/kdash-rs/kdash/archive/refs/tags/v2.1.1.tar.gz"
  end

  def install
    bin.install "kdash.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/kdash --version 2>&1")
  end
end
