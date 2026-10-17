class Detox < Formula
  desc "Utility to replace problematic characters in filenames"
  homepage "https://detox.sourceforge.net/"
  url "https://github.com/dharple/detox/releases/download/v3.0.1/detox-3.0.1.zip"
  sha256 "425a02fda04103b86abe7f83bdd4c73de5c9c9f69041fb16e9e2e602dd78495b"
  license "BSD-3-Clause"

  deprecate! date: "2026-07-28", because: :unmaintained

  bottle do
    sha256 cellar: :any, x86_64_windows: "3afdd41bacdf68e43199450f21ba6bb3d93064f27f55d2b1ffadb480851de3e0"
  end

  depends_on "pkgconf" => :build

  def install
    system "./configure", "--mandir=#{man}", *std_configure_args
    system "make", "install"
  end

  test do
    (testpath/"rename this").write "foobar"
    assert_equal "rename this -> rename_this\n", shell_output("#{bin}/detox --dry-run rename\\ this")
  end
end
