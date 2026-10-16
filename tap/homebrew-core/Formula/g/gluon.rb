class Gluon < Formula
  desc "Static, type inferred and embeddable language written in Rust"
  homepage "https://gluon-lang.org"
  url "https://github.com/gluon-lang/gluon/archive/refs/tags/v0.18.4.tar.gz"
  sha256 "2d600af19c69efcd9412882e9d8f01e5842e2565a2d054772588e4aaffd7ec2f"
  license "MIT"
  head "https://github.com/gluon-lang/gluon.git", branch: "master"

  # There's a lot of false tags here.
  # Those prefixed with 'v' seem to be ok.
  livecheck do
    url :stable
    regex(/^v(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "f6ec4da97515039c82b5124997fa6411f6407860bc84bc59f4557aac63d53211"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args(path: "repl")
  end

  test do
    (testpath/"test.glu").write <<~EOS
      let io = import! std.io
      io.print "Hello world!\\n"
    EOS
    assert_equal "Hello world!\n", shell_output("#{bin}/gluon test.glu")
  end
end
