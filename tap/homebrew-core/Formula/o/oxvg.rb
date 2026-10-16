class Oxvg < Formula
  desc "Fastest SVG toolchain for optimisation, minification, linting, and actions"
  homepage "https://github.com/noahbald/oxvg"
  url "https://github.com/noahbald/oxvg/archive/refs/tags/v0.0.9.tar.gz"
  sha256 "9f6fbc2385784bcca8a3411fbbd7deb690799d0866bc5f5d7cd88756b7746af6"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "52423f71b9910c5d849bab1769c9827199e49d34cabb956a3b6e7ab9e26ab159"
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
