class Stylelint < Formula
  desc "Modern CSS linter"
  homepage "https://stylelint.io/"
  url "https://registry.npmjs.org/stylelint/-/stylelint-18.1.0.tgz"
  sha256 "d0b13361093bd37263a68ee851407ee18f112efa438c96bd0b0c385bf9c7c2a9"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "71795473a4d9551588131e3421055ef7a2e6b83090bad6e2612e703bb3bbee5f"
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
