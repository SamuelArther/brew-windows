class PodcastDl < Formula
  desc "CLI for downloading and archiving podcasts"
  homepage "https://github.com/lightpohl/podcast-dl"
  url "https://registry.npmjs.org/podcast-dl/-/podcast-dl-12.1.7.tgz"
  sha256 "ebcd417e6ee2656aaef72438798cbc36069f1528f11dc65f5c100261df15f1a4"
  license "MIT"
  head "https://github.com/lightpohl/podcast-dl.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "c40103d690fe83125604214dcf713dcf966102a1906017da5b337d6a07ed96e4"
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
