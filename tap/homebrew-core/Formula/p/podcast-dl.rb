class PodcastDl < Formula
  desc "CLI for downloading and archiving podcasts"
  homepage "https://github.com/lightpohl/podcast-dl"
  url "https://registry.npmjs.org/podcast-dl/-/podcast-dl-12.1.5.tgz"
  sha256 "4b6ade9da8d9af1acc22ccc037afbd02af584d0bb7aa6c235a6c58e31fede4eb"
  license "MIT"
  head "https://github.com/lightpohl/podcast-dl.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "2d7a3d3207a74851e078167bee0e37ecd68bdece3ad296044b8c35aa398aa3c9"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    (testpath/"feed.xml").write <<~XML
      <?xml version="1.0" encoding="UTF-8"?>
      <rss version="2.0">
        <channel>
          <title>Homebrew Test Podcast</title>
          <link>https://example.com/podcast</link>
          <description>Fixture for formula testing.</description>
          <item>
            <title>Episode One</title>
            <guid>episode-1</guid>
            <enclosure url="https://example.com/episode.mp3" type="audio/mpeg" length="1"/>
          </item>
        </channel>
      </rss>
    XML

    assert_match "Homebrew Test Podcast", shell_output("#{bin}/podcast-dl --file feed.xml --info")
  end
end
