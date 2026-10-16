class Ssh3 < Formula
  desc "Faster and richer secure shell using HTTP/3"
  homepage "https://arxiv.org/abs/2312.08396"
  url "https://github.com/francoismichel/ssh3/releases/download/v0.1.7/ssh3_0.1.7_clientonly_windows_x86_64.zip"
  sha256 "81b2166fea113b4978b51d5d6fa4d9e23dd0b18c8409d16a5dcb6a97c5bef3bb"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/francoismichel/ssh3/archive/refs/tags/v0.1.7.tar.gz"
  end

  def install
    bin.install "ssh3.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ssh3 --version 2>&1")
  end
end
