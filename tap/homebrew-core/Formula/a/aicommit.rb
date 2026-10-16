class Aicommit < Formula
  desc "AI-powered commit message generator"
  homepage "https://github.com/coder/aicommit"
  url "https://github.com/coder/aicommit/releases/download/v0.6.5/aicommit_0.6.5_windows_amd64.tar.gz"
  sha256 "4fbb0b085d9777792c0651be0dc1bc3ca21744ccf94e4e5090270ab1586a5e7a"
  license "CC0-1.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/coder/aicommit/archive/refs/tags/v0.6.5.tar.gz"
  end

  def install
    bin.install "aicommit.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/aicommit version 2>&1")
  end
end
