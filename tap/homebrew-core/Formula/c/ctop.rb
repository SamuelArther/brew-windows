class Ctop < Formula
  desc "Top-like interface for container metrics"
  homepage "https://bcicen.github.io/ctop/"
  url "https://github.com/bcicen/ctop.git",
      tag:      "v0.7.7",
      revision: "11a1cb10f416b4ca5e36c22c1acc2d11dbb24fb4"
  license "MIT"
  head "https://github.com/bcicen/ctop.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "287703a46df14d62677138eb24cd763a95b145c56593fb9285d7a534e32088ea"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "make", "build"
    bin.install "ctop"
  end

  test do
    system bin/"ctop", "-v"
  end
end
