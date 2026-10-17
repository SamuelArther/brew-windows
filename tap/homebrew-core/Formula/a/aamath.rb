class Aamath < Formula
  desc "Renders mathematical expressions as ASCII art"
  homepage "https://web.archive.org/web/20260501180020/http://fuse.superglue.se/aamath/"
  url "https://cdn.netbsd.org/pub/pkgsrc/distfiles/aamath-0.3.tar.gz"
  mirror "http://fuse.superglue.se/aamath/aamath-0.3.tar.gz"
  sha256 "9843f4588695e2cd55ce5d8f58921d4f255e0e65ed9569e1dcddf3f68f77b631"
  license "GPL-2.0-only"

  # Last release on 2005-06-22 and requires non-upstreamed patch to build.
  # As of deprecation date, homepage is down and HTTP-only.
  deprecate! date: "2026-07-17", because: :unmaintained
  disable! date: "2027-01-05", because: :unmaintained

  bottle do
    sha256 cellar: :any, x86_64_windows: "98dd1aae329c7335d044d7cffbc8577caa10da5abbd831223be7ff399865f425"
  end

  uses_from_macos "bison" => :build # for yacc
  uses_from_macos "flex" => :build
  uses_from_macos "libedit" # readline's license is incompatible with GPL-2.0-only

  # Fix build on clang; patch by Homebrew team
  # https://github.com/Homebrew/homebrew/issues/23872
  patch do
    file "Patches/aamath/0.3.patch"
    type :unofficial
  end

  deny_network_access!

  def install
    unless OS.mac?
      inreplace "Makefile" do |s|
        s.change_make_var! "CFLAGS", "#{s.get_make_var("CFLAGS")} -I#{formula_opt_libexec("libedit")}/include"
        s.change_make_var! "LFLAGS", "#{s.get_make_var("LFLAGS")} -L#{formula_opt_libexec("libedit")}/lib"
      end
    end

    ENV.deparallelize
    system "make"

    bin.install "aamath"
    man1.install "aamath.1"
    prefix.install "testcases"
  end

  test do
    s = pipe_output(bin/"aamath", (prefix/"testcases").read, 0)
    assert_match "f(x + h) = f(x) + h f'(x)", s
  end
end
