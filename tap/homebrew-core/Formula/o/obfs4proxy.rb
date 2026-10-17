class Obfs4proxy < Formula
  desc "Pluggable transport proxy for Tor, implementing obfs4"
  homepage "https://gitlab.com/yawning/obfs4"
  url "https://gitlab.com/yawning/obfs4/-/archive/obfs4proxy-0.0.14/obfs4-obfs4proxy-0.0.14.tar.gz"
  sha256 "a4b7520e732b0f168832f6f2fdf1be57f3e2cce0612e743d3f6b51341a740903"
  license "BSD-2-Clause"
  head "https://gitlab.com/yawning/obfs4.git", branch: "master"

  livecheck do
    url :stable
    regex(/^obfs4proxy[._-]v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "92c078cf4e62c20e8c7d4eeaecc8109e9f3072547b8a6fee0e915e40b7a84e5e"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args, "./obfs4proxy"
  end

  test do
    expect = "ENV-ERROR no TOR_PT_STATE_LOCATION environment variable"
    actual = shell_output("TOR_PT_MANAGED_TRANSPORT_VER=1 TOR_PT_SERVER_TRANSPORTS=obfs4 #{bin}/obfs4proxy", 1)
    assert_match expect, actual
  end
end
