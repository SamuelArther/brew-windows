class Flyscrape < Formula
  desc "Standalone and scriptable web scraper"
  homepage "https://flyscrape.com/"
  url "https://github.com/philippta/flyscrape/archive/refs/tags/v0.9.0.tar.gz"
  sha256 "cbc8c977c55f9617ce29f2178c00c22bda4bd9d1987f37c688580c2848653e17"
  license "MPL-2.0"
  head "https://github.com/philippta/flyscrape.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "6d2bd57a21ad33495f9359f203e7271c1c9f67023f8e5486b48f40fe5bd9c689"
  end

  depends_on "go" => :build

  uses_from_macos "sqlite"

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    tags = "osusergo,netgo,sqlite_omit_load_extension"
    system "go", "build", *std_go_args(tags:), "./cmd/flyscrape"

    pkgshare.install "examples"
  end

  test do
    cp pkgshare/"examples/hackernews.js", testpath
    output = shell_output("#{bin}/flyscrape run hackernews.js 2>&1")
    assert_match "\"url\": \"https://news.ycombinator.com/\"", output
  end
end
