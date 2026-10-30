class Range2cidr < Formula
  desc "Converts IP ranges to CIDRs"
  homepage "https://ipinfo.io"
  url "https://github.com/ipinfo/cli/archive/refs/tags/range2cidr-3.3.4.tar.gz"
  sha256 "003fccb293c9ff8ee42494e7ab07f3be8f9f7e4745ab848e529cac9f89cf3a71"
  license "Apache-2.0"

  livecheck do
    url :stable
    regex(/^range2cidr[._-]v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "0bb3781a1c6873ff142589205657dd51d5465b3575d298d3f0d4ec4d5f735f0c"
  end

  depends_on "go" => :build

  deny_network_access!

  def install
    system "go", "build", *std_go_args, "./range2cidr"
  end

  test do
    assert_equal version.to_s, shell_output("#{bin}/range2cidr --version").chomp
    assert_equal "1.1.1.0/30", shell_output("#{bin}/range2cidr 1.1.1.0-1.1.1.3").chomp
    assert_equal "0.0.0.0/0", shell_output("#{bin}/range2cidr 0.0.0.0-255.255.255.255").chomp
    assert_equal "1.1.1.0/31\n1.1.1.2/32", shell_output("#{bin}/range2cidr 1.1.1.0-1.1.1.2").chomp
  end
end
