class Wuchale < Formula
  desc "Protobuf-like i18n from plain code"
  homepage "https://wuchale.dev/"
  url "https://registry.npmjs.org/wuchale/-/wuchale-0.26.7.tgz"
  sha256 "f3ee8f54491ebca2438f8f22e1db675890547dc38bbc5d985494e1662e1a2900"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "bccd46f9a6e84568da82b0ff386e308ffcc3d44178f340b78ca2525ffb261830"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    (testpath/"wuchale.config.mjs").write <<~EOS
      export default {
        locales: ["en"]
      };
    EOS

    output = shell_output("#{bin}/wuchale --config #{testpath}/wuchale.config.mjs status 2>&1", 1)
    assert_match "at least one adapter is needed.", output
  end
end
