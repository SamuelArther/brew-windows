class Semver < Formula
  desc "Semantic version parser for node (the one npm uses)"
  homepage "https://semver.npmjs.com/"
  url "https://github.com/npm/node-semver/archive/refs/tags/v7.8.5.tar.gz"
  sha256 "0e31552648ead32c3d56f0df70db8db9e4f979720be7b6929aa98a98bd36529f"
  license "ISC"

  bottle do
    sha256 cellar: :any, x86_64_windows: "9a2dbc7c7ac6a9a1a6ae99670e678760d4fb44d2db636fb8d549c97f1ca71e8b"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/semver --help")
    assert_match "1.2.3", shell_output("#{bin}/semver 1.2.3-beta.1 -i release")
  end
end
