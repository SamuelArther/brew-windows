class Dashing < Formula
  desc "Generate Dash documentation from HTML files"
  homepage "https://github.com/technosophos/dashing"
  url "https://github.com/technosophos/dashing/archive/refs/tags/0.4.0.tar.gz"
  sha256 "81b21acae83c144f10d9eea05a0b89f0dcdfa694c3760c2a25bd4eab72a2a3b9"
  license "MIT"
  revision 1
  head "https://github.com/technosophos/dashing.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "98ba068855592ec5bd26c942562e3761808b43bfaa0839f2cb3cf372c388a696"
  end

  depends_on "go" => :build

  resource "redux_saga_docs_tarball" do
    url "https://github.com/dmitrytut/redux-saga-docset/archive/7df9e3070934c0f4b92d66d2165312bf78ecd6a0.tar.gz"
    sha256 "08e5cc1fc0776fd60492ae90961031b1419ea6ed02e2c2d9db2ede67d9d67852"
  end

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ENV["CGO_ENABLED"] = "1" if OS.linux? && Hardware::CPU.arm?

    system "go", "build", *std_go_args(ldflags: "-X main.version=#{version}")
  end

  test do
    # Make sure that dashing creates its settings file and then builds an actual
    # docset for Dash
    testpath.install resource("redux_saga_docs_tarball")
    innerpath = testpath
    system bin/"dashing", "create"
    assert_path_exists innerpath/"dashing.json"
    system bin/"dashing", "build", "."
    innerpath /= "dashing.docset/Contents"
    assert_path_exists innerpath/"Info.plist"
    innerpath /= "Resources"
    assert_path_exists innerpath/"docSet.dsidx"
    innerpath /= "Documents"
    assert_path_exists innerpath/"README.md"
    innerpath /= "docs"
    assert_path_exists innerpath/"index.html"
    innerpath /= "introduction"
    assert_path_exists innerpath/"SagaBackground.html"
  end
end
