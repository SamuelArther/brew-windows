class Goreleaser < Formula
  desc "Deliver Go binaries as fast and easily as possible"
  homepage "https://goreleaser.com/"
  url "https://github.com/goreleaser/goreleaser/releases/download/v2.18.3/goreleaser_Windows_x86_64.zip"
  sha256 "5e7455df2accbfe69416cf9ccbc6aefe9abd2543525d5d6da24426d827aa8171"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "goreleaser.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/goreleaser -v")
  end
end
