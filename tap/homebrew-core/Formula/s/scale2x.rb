class Scale2x < Formula
  desc "Real-time graphics effect"
  homepage "https://www.scale2x.it/"
  url "https://github.com/amadvance/scale2x/releases/download/v4.0/scale2x-4.0.tar.gz"
  sha256 "996f2673206c73fb57f0f5d0e094d3774f595f7e7e80fcca8cc045e8b4ba6d32"
  license "GPL-2.0-or-later"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "d06fac759dcd0cda568904d48e9c923836c5a36f0eaeb865e6e28cec7c7bf531"
  end

  depends_on "libpng"

  on_linux do
    depends_on "zlib-ng-compat"
  end

  deny_network_access!

  def install
    system "./configure", *std_configure_args
    system "make", "install"
  end

  test do
    system bin/"scalex", "-k", "2", test_fixtures("test.png"), "out.png"
    assert_path_exists testpath/"out.png"
  end
end
