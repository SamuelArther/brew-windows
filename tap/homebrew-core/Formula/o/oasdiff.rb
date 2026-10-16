class Oasdiff < Formula
  desc "OpenAPI Diff and Breaking Changes"
  homepage "https://www.oasdiff.com/"
  url "https://github.com/oasdiff/oasdiff/releases/download/v1.33.0/oasdiff_1.33.0_windows_amd64.tar.gz"
  sha256 "22f98c7247075f8a446e783595802d6d38637de1760df264f3d4b3309f766c5b"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/oasdiff/oasdiff/archive/refs/tags/v1.33.0.tar.gz"
    strategy :github_latest
  end

  def install
    bin.install "oasdiff.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/oasdiff --version 2>&1")
  end
end
