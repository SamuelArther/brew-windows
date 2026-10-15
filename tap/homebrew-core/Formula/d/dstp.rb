class Dstp < Formula
  desc "Run common networking tests against your site"
  homepage "https://github.com/ycd/dstp"
  url "https://github.com/ycd/dstp/releases/download/v0.4.23/dstp_0.4.23_Windows_amd64.zip"
  sha256 "e8597c20e924e917718e5102a3e7a0f240c099939ba89a04d07a3f3974955c8e"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/ycd/dstp/archive/refs/tags/v0.4.23.tar.gz"
  end

  def install
    bin.install "dstp.exe"
  end

  test do
    assert_match "HTTPS: got 200 OK", shell_output("#{bin}/dstp example.com --dns 1.1.1.1")
  end
end
