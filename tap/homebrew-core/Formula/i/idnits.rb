class Idnits < Formula
  desc "Looks for problems in internet draft formatting"
  homepage "https://author-tools.ietf.org/idnits3/"
  url "https://registry.npmjs.org/@ietf-tools/idnits/-/idnits-3.2.0.tgz"
  sha256 "ae59abef0eb022dba2d6682c40532880e9b11b45511a242c4934ee5b6542c4ef"
  license "BSD-3-Clause"

  livecheck do
    url "https://registry.npmjs.org/@ietf-tools/idnits/latest"
    strategy :json do |json|
      json["version"]
    end
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "97e0fe14b805f3bb6d47a007daba6f3b64db82d64b9857d91dcc2112fe6797c8"
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
