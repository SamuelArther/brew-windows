class Tnef < Formula
  desc "Microsoft MS-TNEF attachment unpacker"
  homepage "https://github.com/verdammelt/tnef"
  url "https://github.com/verdammelt/tnef/archive/refs/tags/1.4.18.tar.gz"
  sha256 "fa56dd08649f51b173017911cae277dc4b2c98211721c2a60708bf1d28839922"
  license "GPL-2.0-or-later"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "1033e8693c2523ed954d8c639a7a884a3a4e047344a402cb67972dc12a119bb2"
  end

  depends_on "autoconf" => :build
  depends_on "automake" => :build
  depends_on "libtool" => :build

  # Fix `attr_read` prototype that conflicts under C23, which autoconf 2.73 selects
  patch do
    url "https://github.com/verdammelt/tnef/commit/86bfa75cfacbe71c8d5282fa0065981b4544c5ad.patch?full_index=1"
    sha256 "b5828b26840cc01c470852caaee5e375a89fcacc3a3b1c7aacf4b7a0ff0053b1"
    type :backport
    resolves "https://github.com/verdammelt/tnef/pull/49"
  end

  def install
    system "autoreconf", "--force", "--install", "--verbose"
    system "./configure", *std_configure_args
    system "make", "install"
  end

  test do
    system bin/"tnef", "--version"
  end
end
