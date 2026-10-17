class Mjml < Formula
  desc "JavaScript framework that makes responsive-email easy"
  homepage "https://mjml.io"
  url "https://registry.npmjs.org/mjml/-/mjml-5.4.1.tgz"
  sha256 "8cb5e04d3ec9b9b150197a6d9933e17adc9736c8c773268261a57cd8692d00f4"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "88dfe5d70c1f20295f7c4d73257a5fa9590b0e2cd1d5b1829f2ae87999643d40"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    (testpath/"input.mjml").write <<~HTML
      <mjml>
        <mj-body>
          <mj-section>
            <mj-column>
              <mj-text>
                Hello Homebrew!
              </mj-text>
            </mj-column>
          </mj-section>
        </mj-body>
      </mjml>
    HTML
    compiled_html = shell_output("#{bin}/mjml input.mjml")
    assert_match "Hello Homebrew!", compiled_html

    assert_equal <<~EOS, shell_output("#{bin}/mjml --version")
      mjml-core: #{version}
      mjml-cli: #{version}
    EOS
  end
end
