class Oxvg < Formula
  desc "Fastest SVG toolchain for optimisation, minification, linting, and actions"
  homepage "https://github.com/noahbald/oxvg"
  url "https://github.com/noahbald/oxvg/archive/refs/tags/v0.0.10.tar.gz"
  sha256 "9e19bcf36a543d48c25ee52927d46ca75bc8aa1f13b89ee0c1c994a609b7c734"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "2ee499e6d3fecb744ccfb62f16ee92dba7e9b7765df7339211b0a103eb5a92b4"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args(path: "crates/oxvg")
  end

  test do
    input = '<svg><path d="m0 0l0 1"/></svg>'
    assert_equal '<svg><path d="M0 0v1"/></svg>', pipe_output("#{bin}/oxvg optimise", input, 0)
  end
end
