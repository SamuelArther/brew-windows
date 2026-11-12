class Gowsdl < Formula
  desc "WSDL2Go code generation as well as its SOAP proxy"
  homepage "https://github.com/hooklift/gowsdl"
  url "https://github.com/hooklift/gowsdl.git",
      tag:      "v0.5.0",
      revision: "51f3ef6c0e8f41ed1bdccce4c04e86b6769da313"
  license "MPL-2.0"
  head "https://github.com/hooklift/gowsdl.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "14815cd551a2bdacf60096c7bfa393663d153b4185edd37e8cc4fe30e3d19320"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "make", "build"
    bin.install "build/gowsdl"
  end

  test do
    system bin/"gowsdl"
  end
end
