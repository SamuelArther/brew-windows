class Stylelint < Formula
  desc "Modern CSS linter"
  homepage "https://stylelint.io/"
  url "https://registry.npmjs.org/stylelint/-/stylelint-17.16.0.tgz"
  sha256 "950b26ca89d53ea55cec1b8ca904ff6aa0d93007d360fe840dffdc174e148f66"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "7d08b42110fd3e8079c56c0fc6aa8ac85bb62bb706aab687055016ab8a0fa7c5"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    (testpath/".stylelintrc").write <<~JSON
      {
        "rules": {
          "block-no-empty": true
        }
      }
    JSON

    (testpath/"test.css").write <<~CSS
      a {
      }
    CSS

    output = shell_output("#{bin}/stylelint test.css 2>&1", 2)
    assert_match "Empty block", output

    assert_match version.to_s, shell_output("#{bin}/stylelint --version")
  end
end
