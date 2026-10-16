class Jsonpp < Formula
  desc "Command-line JSON pretty-printer"
  homepage "https://jmhodges.github.io/jsonpp/"
  url "https://github.com/jmhodges/jsonpp/archive/refs/tags/1.3.0.tar.gz"
  sha256 "dde8ea9b270a79cd2b2f40824f89abc5270bd360122d87ab04b4361c0015d941"
  license "MIT"
  revision 1
  head "https://github.com/jmhodges/jsonpp.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "c2fef12723c73ce297f5151945b304ccc2d42870a8c6ab4c8131fea3fa81d799"
  end

  depends_on "go" => :build

  deny_network_access!

  def install
    ENV["GO111MODULE"] = "auto"
    system "go", "build", *std_go_args
  end

  test do
    expected = <<~JSON.chomp
      {
        "foo": "bar",
        "baz": "qux"
      }
    JSON
    assert_equal expected, pipe_output(bin/"jsonpp", '{"foo":"bar","baz":"qux"}')
  end
end
