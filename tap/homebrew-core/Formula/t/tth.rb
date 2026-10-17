class Tth < Formula
  desc "TeX/LaTeX to HTML converter"
  homepage "http://silas.psfc.mit.edu/tth/"
  url "https://downloads.sourceforge.net/project/tth/tth4.16.tar.gz"
  sha256 "b0e118d49a37e06598c1e2b524ea352ceabf064afef25acf02b556229ee43512"
  license "GPL-2.0-only"

  livecheck do
    url :stable
    regex(%r{url=.*?/tth[._-]?v?(\d+(?:\.\d+)+)\.t}i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "835c6c707b644af21e3986c0b2adc0721f8793897c07c47c32698bf1f79b3f8a"
  end

  uses_from_macos "flex" => :build

  def install
    system "make", "tth.c"
    # GCC 16 defaults to C23, where tth's K&R declarations take no arguments.
    args = OS.windows? ? ["-std=gnu17"] : []
    system ENV.cc, *args, "-o", "tth", "tth.c"
    bin.install %w[tth latex2gif ps2gif]
    man1.install "tth.1"
  end

  test do
    assert_match(/version #{version}/, pipe_output(bin/"tth", ""))
  end
end
