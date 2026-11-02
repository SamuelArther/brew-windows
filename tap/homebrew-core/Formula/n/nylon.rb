class Nylon < Formula
  desc "Proxy server"
  homepage "https://github.com/smeinecke/nylon"
  url "https://monkey.org/~marius/nylon/nylon-1.21.tar.gz"
  sha256 "34c132b005c025c1a5079aae9210855c80f50dc51dde719298e1113ad73408a4"
  license "BSD-3-Clause"
  revision 2

  livecheck do
    skip "No version information available to check"
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "8fb14ed8b787eb4576b96eaad0672d789249befdb6b2eb09869edc371e87fb9e"
  end

  depends_on "libevent"

  deny_network_access!

  def install
    args = []
    # Help old config scripts identify arm64 linux
    args << "--build=aarch64-unknown-linux-gnu" if OS.linux? && Hardware::CPU.arm64?

    system "./configure", "--mandir=#{man}", "--with-libevent=#{HOMEBREW_PREFIX}", *args, *std_configure_args
    system "make", "install"
  end

  test do
    assert_equal "nylon: nylon version #{version}",
      shell_output("#{bin}/nylon -V 2>&1").chomp
  end
end
