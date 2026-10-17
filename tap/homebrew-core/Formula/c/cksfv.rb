class Cksfv < Formula
  desc "File verification utility"
  homepage "https://zakalwe.fi/~shd/foss/cksfv/"
  url "https://zakalwe.fi/~shd/foss/cksfv/files/cksfv-1.3.15.tar.bz2"
  sha256 "a173be5b6519e19169b6bb0b8a8530f04303fe3b17706927b9bd58461256064c"
  license "GPL-2.0-or-later"

  livecheck do
    url "https://zakalwe.fi/~shd/foss/cksfv/files/"
    regex(/href=.*?cksfv[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/cksfv-1.3.15"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "2ad248d25180893ba8163921dd633b314e98aca4d68384e65efca33b2ef3e4b4"
  end

  def install
    system "./configure", "--prefix=#{prefix}"
    system "make", "install"
  end

  test do
    path = testpath/"foo"
    path.write "abcd"

    assert_match "#{path} ED82CD11", shell_output("#{bin}/cksfv #{path}")
  end
end
