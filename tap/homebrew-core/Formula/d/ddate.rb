class Ddate < Formula
  desc "Converts boring normal dates to fun Discordian Date"
  homepage "https://github.com/bo0ts/ddate"
  url "https://github.com/bo0ts/ddate/archive/refs/tags/v0.2.2.tar.gz"
  sha256 "d53c3f0af845045f39d6d633d295fd4efbe2a792fd0d04d25d44725d11c678ad"
  license :public_domain

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/ddate-0.2.2"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "3d65cdb9a5ac5bbac1e8056666f19d1a55f9456ee2defed493a5b9c1520cf06c"
  end

  def install
    system ENV.cc, "ddate.c", "-o", "ddate"
    bin.install "ddate"
    man1.install "ddate.1"
  end

  test do
    output = shell_output("#{bin}/ddate 20 6 2014").strip
    assert_equal "Sweetmorn, Confusion 25, 3180 YOLD", output
  end
end
