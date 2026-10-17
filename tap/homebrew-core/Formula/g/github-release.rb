class GithubRelease < Formula
  desc "Create and edit releases on Github (and upload artifacts)"
  homepage "https://github.com/github-release/github-release"
  url "https://github.com/github-release/github-release/archive/refs/tags/v0.11.0.tar.gz"
  sha256 "6e6d3d77ce7d6acf6c4e4e0e005c71641ba7c2eb67d9f1f2e2cf2cc5f5c68086"
  license "MIT"
  head "https://github.com/github-release/github-release.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "3d75c4499e8bba25e5d9258f246fcc46ee067535f00d94bd0b7cb90dc99cc19f"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args
  end

  test do
    assert_match "github-release v#{version}", shell_output("#{bin}/github-release --version")

    system bin/"github-release", "info", "--user", "github-release",
                                         "--repo", "github-release",
                                         "--tag",  "v#{version}"
  end
end
