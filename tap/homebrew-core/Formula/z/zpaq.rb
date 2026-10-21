class Zpaq < Formula
  desc "Incremental, journaling command-line archiver"
  homepage "https://mattmahoney.net/dc/zpaq.html"
  url "https://mattmahoney.net/dc/zpaq715.zip"
  version "7.15"
  sha256 "e85ec2529eb0ba22ceaeabd461e55357ef099b80f61c14f377b429ea3d49d418"
  license all_of: ["Unlicense", "MIT", :public_domain]
  revision 1
  head "https://github.com/zpaq/zpaq.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "fff5d901359e4ce2f7259ac47820d7d9679b56d2bf652a5772ade2262ca96659"
  end

  resource "test", :test do
    url "https://mattmahoney.net/dc/calgarytest2.zpaq"
    sha256 "b110688939477bbe62263faff1ce488872c68c0352aa8e55779346f1bd1ed07e"
  end

  deny_network_access!

  def install
    # When building on non-Intel this is supposed to be manually uncommented
    # from the Makefile!  (It's also missing "-D" though)
    inreplace "Makefile", "# CPPFLAGS+=NOJIT", "CPPFLAGS+=-DNOJIT" unless Hardware::CPU.intel?
    system "make"
    system "make", "check"
    system "make", "install", "PREFIX=#{prefix}"
  end

  test do
    testpath.install resource("test")
    assert_match "all OK", shell_output("#{bin}/zpaq x calgarytest2.zpaq 2>&1")
  end
end
