class WikibaseCli < Formula
  desc "Command-line interface to Wikibase"
  homepage "https://codeberg.org/maxlath/wikibase-cli"
  url "https://registry.npmjs.org/wikibase-cli/-/wikibase-cli-20.3.2.tgz"
  sha256 "919d18a4d1985e6d562d8681410679565122e5994e70ee28a0f00f40cde8b02d"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "295af3fd231636c0ba58188436c1d3b37a45b1c8f1bdb93e2c1316e1dd9c6be8"
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
