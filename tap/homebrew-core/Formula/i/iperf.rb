class Iperf < Formula
  desc "Tool to measure maximum TCP and UDP bandwidth"
  homepage "https://sourceforge.net/projects/iperf2/"
  url "https://repo.msys2.org/msys/x86_64/iperf-2.2.1-2-x86_64.pkg.tar.zst"
  sha256 "e1f13151f8744645a9bb1e4d471392089d2590fdb91995da7848d7bb200f552e"
  license "BSD-3-Clause"

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url "https://downloads.sourceforge.net/project/iperf2/iperf-2.2.1.tar.gz"
    regex(%r{url=.*?/iperf[._-]v?(\d+(?:\.\d+)+)\.t}i)
  end

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/iperf.exe", "iperf"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/iperf --version 2>&1")
  end
end
