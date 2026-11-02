class MecabKo < Formula
  desc "See mecab"
  homepage "https://bitbucket.org/eunjeon/mecab-ko"
  url "https://bitbucket.org/eunjeon/mecab-ko/downloads/mecab-0.996-ko-0.9.2.tar.gz"
  version "0.996-ko-0.9.2"
  sha256 "d0e0f696fc33c2183307d4eb87ec3b17845f90b81bf843bd0981e574ee3c38cb"
  license any_of: ["GPL-2.0-only", "LGPL-2.1-only", "BSD-3-Clause"]
  compatibility_version 1

  livecheck do
    skip "No longer developed or maintained"
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "05873551777631da07ebb7aa52e720a4e7f5de6eeb00c07317a230fa7b380f38"
  end

  conflicts_with "mecab", because: "both install mecab binaries"

  deny_network_access!

  def install
    args = ["--sysconfdir=#{etc}"]
    # Help old config scripts identify arm64 linux
    args << "--build=aarch64-unknown-linux-gnu" if OS.linux? && Hardware::CPU.arm64?

    system "./configure", *args, *std_configure_args
    system "make", "install"

    # Put dic files in HOMEBREW_PREFIX/lib instead of lib
    inreplace [bin/"mecab-config", etc/"mecabrc"], "#{lib}/mecab/dic", "#{HOMEBREW_PREFIX}/lib/mecab/dic"
  end

  post_install_steps do
    mkdir_p "{{HOMEBREW_PREFIX}}/lib/mecab/dic"
  end

  test do
    assert_equal "#{HOMEBREW_PREFIX}/lib/mecab/dic", shell_output("#{bin}/mecab-config --dicdir").chomp
  end
end
