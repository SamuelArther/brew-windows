class Pint < Formula
  desc "Prometheus rule linter/validator"
  homepage "https://cloudflare.github.io/pint/"
  url "https://github.com/cloudflare/pint/releases/download/v0.89.2/pint-0.89.2-windows-amd64.tar.gz"
  sha256 "fbf7365738da25b7dd7efb53182bf2d9120835014bc5b794a54afa507ffcd55a"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/cloudflare/pint/archive/refs/tags/v0.89.2.tar.gz"
  end

  def install
    bin.install "pint-windows-amd64.exe" => "pint.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/pint version 2>&1")
  end
end
