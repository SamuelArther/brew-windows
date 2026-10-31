class Clpbar < Formula
  desc "Command-line progress bar"
  homepage "https://clpbar.sourceforge.net/"
  url "https://downloads.sourceforge.net/project/clpbar/clpbar/bar-1.11.1/bar_1.11.1.tar.gz"
  sha256 "fa0f5ec5c8400316c2f4debdc6cdcb80e186e668c2e4471df4fec7bfcd626503"
  license "GPL-2.0-or-later"

  deny_network_access!

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "f41ba044544edc6162da0652ff6e377399ec56ecaf266cec426f7006ca3199fd"
  end

  def install
    args = []
    # Help old config scripts identify arm64 linux
    args << "--build=aarch64-unknown-linux-gnu" if OS.linux? && Hardware::CPU.arm64?

    system "./configure", "--program-prefix='clp'", *args, *std_configure_args
    system "make", "install"
  end

  test do
    output = pipe_output("#{bin}/clpbar 2>&1", shell_output("dd if=/dev/zero bs=1024 count=5"))
    assert_match "Copied: 5120B (5.0KB)", output
  end
end
