class Hoedown < Formula
  desc "Secure Markdown processing (a revived fork of Sundown)"
  homepage "https://github.com/hoedown/hoedown"
  url "https://github.com/hoedown/hoedown/archive/refs/tags/3.0.7.tar.gz"
  sha256 "01b6021b1ec329b70687c0d240b12edcaf09c4aa28423ddf344d2bd9056ba920"
  license "ISC"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/hoedown-3.0.7"
    sha256 cellar: :any, x86_64_windows: "f26c22019774bf2b25e2f349444f574551b471f3e7c99087a413318b57b6928a"
  end

  def install
    system "make", "hoedown"
    bin.install "hoedown"
    prefix.install "test"
  end

  test do
    system "perl", "#{prefix}/test/MarkdownTest_1.0.3/MarkdownTest.pl",
                   "--script=#{bin}/hoedown",
                   "--testdir=#{prefix}/test/MarkdownTest_1.0.3/Tests",
                   "--tidy"
  end
end
