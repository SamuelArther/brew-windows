class Rfcstrip < Formula
  desc "Strips headers and footers from RFCs and Internet-Drafts"
  homepage "https://github.com/mbj4668/rfcstrip"
  url "https://github.com/mbj4668/rfcstrip/archive/refs/tags/1.3.tar.gz"
  sha256 "bba42a64535f55bfd1eae0cf0b85f781dacf5f3ce323b16515f32cefff920c6b"
  # License is similar to TCL license but omits the government use clause
  license :cannot_represent

  bottle do
    sha256 cellar: :any, x86_64_windows: "6c736f74eb6b56bc278221291565623ff87c9de7d92337b69df8aba9b930c8c0"
  end

  resource "rfc1149", :test do
    url "https://www.ietf.org/rfc/rfc1149.txt"
    sha256 "a8660fa4f47bd5e3db1cd5d5baad983d8b6f3f1e8a1a04b8552f3c2ce8f33c18"
  end

  def install
    bin.install "rfcstrip"
  end

  test do
    resource("rfc1149").stage testpath
    stripped = shell_output("#{bin}/rfcstrip rfc1149.txt")
    refute_match(/\[Page \d+\]/, stripped) # RFC page numbering
    refute_match "\f", stripped # form feed a.k.a. Control-L
  end
end
