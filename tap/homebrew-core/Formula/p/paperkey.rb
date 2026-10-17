class Paperkey < Formula
  desc "Extract just secret information out of OpenPGP secret keys"
  homepage "https://www.jabberwocky.com/software/paperkey/"
  url "https://www.jabberwocky.com/software/paperkey/paperkey-1.6.tar.gz"
  sha256 "a245fd13271a8d2afa03dde979af3a29eb3d4ebb1fbcad4a9b52cf67a27d05f7"
  license "GPL-2.0-or-later"

  livecheck do
    url :homepage
    regex(/href=.*?paperkey[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "21de5f2cfcd169ff7ec7573ea61f08c309ad7d17bb0f3df221df3d09d4041803"
  end

  resource "homebrew-test_sec", :test do
    url "https://raw.githubusercontent.com/dmshaw/paperkey/46adad971458a798e203bf8ec65d6bc897494754/checks/papertest-rsa.sec"
    sha256 "0f39397227339171209760e0f27aa60ecf7eae31c32d0ec3a358434afd38eacd"
  end

  def install
    args = []
    # Help old config scripts identify arm64 linux
    args << "--build=aarch64-unknown-linux-gnu" if OS.linux? && Hardware::CPU.arm? && Hardware::CPU.is_64_bit?

    system "./configure", *args, *std_configure_args
    system "make", "install"
  end

  test do
    resource("homebrew-test_sec").stage do
      system bin/"paperkey", "--secret-key", "papertest-rsa.sec", "--output", "test"
      assert_path_exists Pathname.pwd/"test"
    end
  end
end
