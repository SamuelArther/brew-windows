class WormScraper < Formula
  desc "Scrape Worm, Ward, and Glow-worm web serials into EPUB ebooks"
  homepage "https://github.com/domenic/worm-scraper"
  url "https://registry.npmjs.org/worm-scraper/-/worm-scraper-9.2.2.tgz"
  sha256 "c8ead11dfbfeeed92c6e02fd9de88c6ef293564e017ab678d3ebf7d3f9f6c4c1"
  license "WTFPL"
  head "https://github.com/domenic/worm-scraper.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "cf24d38c5d534a37472ddd1fa209feafcfd7fd52b3e42d4444057e60a93cb6ca"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")

    # Remove incompatible pre-built binaries
    os = OS.kernel_name.downcase
    arch = Hardware::CPU.intel? ? "x64" : Hardware::CPU.arch.to_s
    node_modules = libexec/"lib/node_modules/worm-scraper/node_modules"
    node_modules.glob("{bare-fs,bare-path,bare-url}/prebuilds/*")
                .each { |dir| rm_r(dir) if dir.basename.to_s != "#{os}-#{arch}" }
  end

  test do
    staging = testpath/"staging/worm"
    (staging/"OEBPS").mkpath
    (staging/"META-INF").mkpath
    (staging/"mimetype").write "application/epub+zip"
    (staging/"OEBPS/chapter.xhtml").write "<html><body>Homebrew</body></html>"
    (staging/"META-INF/container.xml").write "<container/>"

    epub = testpath/"test.epub"
    system bin/"worm-scraper", "zip", "--staging=#{testpath}/staging", "--out=#{epub}"
    assert_path_exists epub
    assert_equal "application/epub+zip", shell_output("unzip -p #{epub} mimetype")
  end
end
