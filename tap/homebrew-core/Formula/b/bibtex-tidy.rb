class BibtexTidy < Formula
  desc "Cleaner and Formatter for BibTeX files"
  homepage "https://flamingtempura.github.io/bibtex-tidy/"
  url "https://registry.npmjs.org/bibtex-tidy/-/bibtex-tidy-1.15.1.tgz"
  sha256 "f911be78ea301c6079f9a90c3c1a42b7c85d130dbbf73fd80a462532fc8b20b8"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "006793a286d1755f85bb40498273b63c909d8a76ca5cdcb2a28396f61a2618a7"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    test_file = testpath/"test.bib"
    test_file.write <<~BIBTEX
      @article{example,
        author = {Author},
        title = {Title},
        year = {2024}
      }
    BIBTEX

    output = shell_output("#{bin}/bibtex-tidy #{test_file}")
    assert_match "Done. Successfully tidied 1 entries.", output
  end
end
