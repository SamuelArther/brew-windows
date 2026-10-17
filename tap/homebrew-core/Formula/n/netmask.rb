class Netmask < Formula
  desc "IP address netmask generation utility"
  homepage "https://github.com/tlby/netmask"
  url "https://github.com/tlby/netmask/releases/download/v2.5.0/netmask-2.5.0.tar.gz"
  sha256 "5975a37fb84ca7035050e10a10fe48322d8ce184b83bc8482f3f3b49882f4c73"
  license "GPL-2.0-or-later"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "8797aa89d12717a459f1e394c4a566f04108dcd6dd43bc46327dcb1838648f2d"
  end

  depends_on "pkgconf" => :build

  depends_on "check"

  on_system :linux, macos: :ventura_or_newer do
    depends_on "texinfo" => :build
  end

  def install
    system "./configure", *std_configure_args
    system "make", "install"
  end

  test do
    assert_equal "100.64.0.0/10", shell_output("#{bin}/netmask -c 100.64.0.0:100.127.255.255").strip
  end
end
