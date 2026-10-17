class Json2tsv < Formula
  desc "JSON to TSV converter"
  homepage "https://codemadness.org/json2tsv.html"
  url "https://codemadness.org/releases/json2tsv/json2tsv-1.2.tar.gz"
  sha256 "113e5a7aeb295e7f8135f231cad900091f99aebd6c98316f761d377e9b50fd84"
  license "ISC"

  livecheck do
    url "https://codemadness.org/releases/json2tsv/"
    regex(/href=.*?json2tsv[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "e0dd860d035bceecc6d608c9fcd6422778d9342fac939fcd238154e4f1b44458"
  end

  conflicts_with "jaq", because: "both install `jaq` binaries"

  def install
    system "make", "install", "PREFIX=#{prefix}", "MANPREFIX=#{man}"
  end

  test do
    input = "{\"foo\": \"bar\", \"baz\": [12, 34]}"
    expected_output = <<~EOS
      \to\t
      .foo\ts\tbar
      .baz\ta\t
      .baz[]\tn\t12
      .baz[]\tn\t34
    EOS

    assert_equal expected_output, pipe_output(bin/"json2tsv", input)
  end
end
