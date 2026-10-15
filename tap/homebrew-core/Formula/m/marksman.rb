class Marksman < Formula
  desc "Language Server Protocol for Markdown"
  homepage "https://github.com/artempyanykh/marksman"
  url "https://github.com/artempyanykh/marksman/releases/download/2026-02-08/marksman.exe"
  version "2026-02-08"
  sha256 "a6d05beb08ebe41b0a9f09c98a438540421436fa5531424c22e0bb1d22529705"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "marksman.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/marksman --version")
  end
end
