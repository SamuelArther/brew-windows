class Fairymax < Formula
  desc "AI for playing Chess variants"
  homepage "https://www.chessvariants.com/index/msdisplay.php?itemid=MSfairy-max"
  url "https://deb.debian.org/debian/pool/main/f/fairymax/fairymax_5.0b.orig.tar.gz"
  sha256 "3e36ba168aa10179225f8dd6953d1c39c4cd4526572765b2e9c77717efe3e52f"
  license :public_domain
  compatibility_version 1

  livecheck do
    skip "No longer developed or maintained"
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "bcfcf85560ba1e667339004b9ddf315500f1513a1f7590b57df84263d6d5b865"
  end

  def install
    # Workaround for newer Clang
    ENV.append_to_cflags "-Wno-implicit-int" if DevelopmentTools.clang_build_version >= 1403

    system "make", "install", "prefix=#{prefix}", "CC=#{ENV.cc}"
  end

  test do
    output = pipe_output(bin/"fairymax", "hint\nquit\n", 0)
    refute_match(/piece-description file .* not found/, output)
  end
end
