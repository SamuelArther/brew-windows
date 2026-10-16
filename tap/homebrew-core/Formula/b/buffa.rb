class Buffa < Formula
  desc "Pure-Rust Protocol Buffers implementation with editions support"
  homepage "https://github.com/anthropics/buffa"
  url "https://github.com/anthropics/buffa/releases/download/v0.9.2/protoc-gen-buffa-packaging-v0.9.2-windows-x86_64.exe"
  sha256 "0db4cdfc67fe8a3b45f3690cc766abdaf2a10f67122c6ce94b46d6c62abd6015"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/anthropics/buffa/archive/refs/tags/v0.9.2.tar.gz"
  end

  def install
    bin.install "protoc-gen-buffa-packaging-v0.9.2-windows-x86_64.exe" => "protoc-gen-buffa.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/protoc-gen-buffa --version 2>&1")
  end
end
