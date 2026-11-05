class WikibaseCli < Formula
  desc "Command-line interface to Wikibase"
  homepage "https://codeberg.org/maxlath/wikibase-cli"
  url "https://registry.npmjs.org/wikibase-cli/-/wikibase-cli-20.3.3.tgz"
  sha256 "81ffbf5c29d3aa17ee6996e2504a76507db9adce0af9dff1b7ac7cc30c0feae7"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "6b0d4889bcf605a08b96ff136ac6f55af2aad321a6dc5b51b75cac842579ed91"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    config_file = testpath/".wikibase-cli.json"
    config_file.write "{\"instance\":\"https://www.wikidata.org\"}"

    ENV["WB_CONFIG"] = config_file

    assert_equal "human", shell_output("#{bin}/wd label Q5 --lang en").strip

    assert_match version.to_s, shell_output("#{bin}/wd --version")
  end
end
