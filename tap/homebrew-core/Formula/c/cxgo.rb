class Cxgo < Formula
  desc "Transpiling C to Go"
  homepage "https://gotranspile.dev"
  url "https://github.com/gotranspile/cxgo/releases/download/v0.5.2/cxgo_0.5.2_windows_amd64.tar.gz"
  sha256 "fe6c172f25a325f9f8b1b198e9e8d8ce7b5c40c4cbc0d6c511ad3af111330bf8"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/gotranspile/cxgo/archive/refs/tags/v0.5.2.tar.gz"
  end

  def install
    bin.install "cxgo.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cxgo version 2>&1")
  end
end
