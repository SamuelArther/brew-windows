class Fetch < Formula
  desc "Download assets from a commit, branch, or tag of GitHub repositories"
  homepage "https://www.gruntwork.io/"
  url "https://github.com/gruntwork-io/fetch/releases/download/v0.4.8/fetch_windows_amd64.exe"
  sha256 "d84b40609ec638d8429f92c736ae9fe7e2d7eb08c8d3198e45f60881a377c5ee"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/gruntwork-io/fetch/archive/refs/tags/v0.4.8.tar.gz"
  end

  def install
    bin.install "fetch_windows_amd64.exe" => "fetch.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/fetch --version 2>&1")
  end
end
