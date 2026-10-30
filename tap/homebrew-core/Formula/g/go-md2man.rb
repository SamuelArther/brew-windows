class GoMd2man < Formula
  desc "Converts markdown into roff (man pages)"
  homepage "https://github.com/cpuguy83/go-md2man"
  url "https://github.com/cpuguy83/go-md2man/archive/refs/tags/v2.0.7.tar.gz"
  sha256 "ca3a5b57e2c01759f5a00ad2a578d034c5370fae9aa7a6c3af5648b2fc802a92"
  license "MIT"
  compatibility_version 1
  head "https://github.com/cpuguy83/go-md2man.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "4c391f63707e547c773d42485c5c6bca5efce37579f2e4748db763e8b7621864"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args
    system bin/"go-md2man", "-in=go-md2man.1.md", "-out=go-md2man.1"
    man1.install "go-md2man.1"
  end

  test do
    assert_includes pipe_output(bin/"go-md2man", "# manpage\nand a half\n"),
                    ".TH manpage\nand a half\n"
  end
end
