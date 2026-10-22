class MarkdownlintCli2 < Formula
  desc "Fast, flexible, config-based cli for linting Markdown/CommonMark files"
  homepage "https://github.com/DavidAnson/markdownlint-cli2"
  url "https://registry.npmjs.org/markdownlint-cli2/-/markdownlint-cli2-0.24.0.tgz"
  sha256 "66ab8e028fd8e6eb64a4f277660142e1c4ea8c61501974d91a060f2586a0c548"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "c7215d8482ac38cccce369820038b6ae009bc9a975b4753275556249b6e13930"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    (testpath/"test-bad.md").write <<~MARKDOWN
      # Header 1
      body
    MARKDOWN
    (testpath/"test-good.md").write <<~MARKDOWN
      # Header 1

      body
    MARKDOWN
    assert_match "Summary: 1 issue",
      shell_output("#{bin}/markdownlint-cli2 :#{testpath}/test-bad.md 2>&1", 1)
    assert_match "Summary: 0 issue",
      shell_output("#{bin}/markdownlint-cli2 :#{testpath}/test-good.md")
  end
end
