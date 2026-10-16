class Dnote < Formula
  desc "Simple command-line notebook"
  homepage "https://www.getdnote.com"
  url "https://github.com/dnote/dnote/releases/download/cli-v0.16.0/dnote_0.16.0_windows_amd64.tar.gz"
  version "0.16.0"
  sha256 "ab7261a44afe8e73e0e71b773c6ebcc15a7468e18a47998ca8776c6fa67ea990"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/dnote/dnote/archive/refs/tags/cli-v0.16.0.tar.gz"
    regex(/^cli[._-]v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "dnote.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/dnote version 2>&1")
  end
end
