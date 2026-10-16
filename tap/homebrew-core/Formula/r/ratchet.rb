class Ratchet < Formula
  desc "Tool for securing CI/CD workflows with version pinning"
  homepage "https://github.com/sethvargo/ratchet"
  url "https://github.com/sethvargo/ratchet/releases/download/v0.12.0/ratchet_0.12.0_windows_amd64.zip"
  sha256 "98658f6a4bf3aef547e8b88133ec57a68edda77fb3f9d49f8666ffe487e9e2cf"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/sethvargo/ratchet/archive/refs/tags/v0.12.0.tar.gz"
  end

  def install
    bin.install "ratchet.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ratchet --version 2>&1")
  end
end
