class Idnits < Formula
  desc "Looks for problems in internet draft formatting"
  homepage "https://author-tools.ietf.org/idnits3/"
  url "https://registry.npmjs.org/@ietf-tools/idnits/-/idnits-3.3.0.tgz"
  sha256 "6103dd8d4e721fd59946e51a462e1fc679e747d4ad805cd55ac3a76c5a2678be"
  license "BSD-3-Clause"

  livecheck do
    url "https://registry.npmjs.org/@ietf-tools/idnits/latest"
    strategy :json do |json|
      json["version"]
    end
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "3a137646b0559b6bb2a2616481c4c0ecd8a35091f7877942c0fe798b77231541"
  end

  depends_on "node"

  resource "homebrew-test", :test do
    url "https://datatracker.ietf.org/doc/id/draft-tian-frr-alt-shortest-path-01.txt"
    sha256 "dd20ac54e5e864cfd426c7fbbbd7a1c200eeff5b7b4538ba3a929d9895f01b76"
  end

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink Dir["#{libexec}/bin/*"]
  end

  test do
    testpath.install resource("homebrew-test")
    output = shell_output("#{bin}/idnits draft-tian-frr-alt-shortest-path-01.txt")
    assert_match(/\d+ errors?/, output)

    assert_match version.to_s, shell_output("#{bin}/idnits --version")
  end
end
