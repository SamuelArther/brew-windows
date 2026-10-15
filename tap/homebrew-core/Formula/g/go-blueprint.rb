class GoBlueprint < Formula
  desc "CLI to streamline Go project setup with standardized structure"
  homepage "https://docs.go-blueprint.dev/"
  url "https://github.com/Melkeydev/go-blueprint/releases/download/v0.10.11/go-blueprint_0.10.11_Windows_x86_64.zip"
  sha256 "176e2bd9e59b4064277e2df064e630e4cc86e94f133dc8f8d3d0fec8a22c87cf"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Melkeydev/go-blueprint/archive/refs/tags/v0.10.11.tar.gz"
  end

  def install
    bin.install "go-blueprint.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/go-blueprint version 2>&1")
  end
end
