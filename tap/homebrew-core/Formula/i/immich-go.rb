class ImmichGo < Formula
  desc "Alternative to the official immich-CLI command written in Go"
  homepage "https://github.com/simulot/immich-go"
  url "https://github.com/simulot/immich-go/releases/download/v0.32.0/immich-go_Windows_x86_64.zip"
  sha256 "37326f95d6654f7f67a5fde3263e96aa8f9cf288dcbb9a2164aafae0aaa144af"
  license "AGPL-3.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/simulot/immich-go/archive/refs/tags/v0.32.0.tar.gz"
    strategy :github_latest
  end

  def install
    bin.install "immich-go.exe"
  end

  test do
    output = shell_output("#{bin}/immich-go --server http://localhost --api-key test upload from-folder . 2>&1", 1)
    assert_match "Error: error while calling the immich's ping API", output

    assert_match version.to_s, shell_output("#{bin}/immich-go --version")
  end
end
