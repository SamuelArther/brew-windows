class Texi2mdoc < Formula
  desc "Convert Texinfo data to mdoc input"
  homepage "https://mandoc.bsd.lv/texi2mdoc/"
  url "https://mandoc.bsd.lv/texi2mdoc/snapshots/texi2mdoc-0.1.2.tgz"
  sha256 "7a45fd87c27cc8970a18db9ddddb2f09f18b8dd5152bf0ca377c3a5e7d304bfe"
  license "ISC"
  head "anoncvs@mandoc.bsd.lv:/cvs", using: :cvs

  livecheck do
    url "https://mandoc.bsd.lv/texi2mdoc/snapshots/"
    regex(/href=.*?texi2mdoc[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "87dfd9c6ca286c89e3e56c6964811a69fcc1fb297bfc1f292020ef53f8d8a2ea"
  end

  def install
    system "make", "install", "PREFIX=#{prefix}"
    share.install prefix/"man"
  end

  test do
    (testpath/"test.texi").write <<~EOS
      @ifnottex
      @node Top
      @top Hello World!
      @end ifnottex
      @bye
    EOS

    output = shell_output("#{bin}/texi2mdoc #{testpath}/test.texi")
    expected_outputs = [/\.Nm\s+test/, /\.Sh\s+Hello World!/]
    expected_outputs.each do |expected|
      assert_match expected, output
    end
  end
end
