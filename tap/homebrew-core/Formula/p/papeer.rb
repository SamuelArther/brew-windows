class Papeer < Formula
  desc "Convert websites into eBooks and Markdown"
  homepage "https://papeer.tech"
  url "https://github.com/lapwat/papeer/releases/download/v0.9.1/papeer-v0.9.1-windows-amd64.zip"
  sha256 "67da4f2e8741414817e43633d87d158cc930ebc75061daf6a83fc1d508a27af9"
  license "GPL-3.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/lapwat/papeer/archive/refs/tags/v0.9.1.tar.gz"
  end

  def install
    bin.install "papeer.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/papeer version 2>&1")
  end
end
