class Parrot < Formula
  desc "Open source virtual machine (for Perl6, et al.)"
  homepage "http://www.parrot.org/"
  license "Artistic-2.0"
  head "https://github.com/parrot/parrot.git", branch: "master"

  stable do
    url "http://ftp.parrot.org/releases/supported/8.1.0/parrot-8.1.0.tar.bz2"
    mirror "https://ftp.osuosl.org/pub/parrot/releases/supported/8.1.0/parrot-8.1.0.tar.bz2"
    sha256 "caf356acab64f4ea50595a846808e81d0be8ada8267afbbeb66ddb3c93cb81d3"

    # remove at 8.2.0, already in HEAD
    patch do
      url "https://github.com/parrot/parrot/commit/7524bf5384ddebbb3ba06a040f8acf972aa0a3ba.patch?full_index=1"
      sha256 "1357090247b856416b23792a2859ae4860ed1336b05dddc1ee00793b6dc3d78a"
      type :backport
      resolves "https://github.com/parrot/parrot/issues/1213"
    end

    # remove at 8.2.0, already in HEAD
    patch do
      url "https://github.com/parrot/parrot/commit/854aec65d6de8eaf5282995ab92100a2446f0cde.patch?full_index=1"
      sha256 "4e068c3a9243f350a3e862991a1042a06a03a625361f9f01cc445a31df906c6e"
      type :backport
      resolves "https://github.com/parrot/parrot/issues/1213"
    end
  end

  # https://github.com/parrot/parrot/commit/f89a111c06ad0367817c52fda6ff5c24165c005b
  deprecate! date: "2025-01-09", because: :unmaintained
  disable! date: "2026-01-09", because: :unmaintained

  uses_from_macos "perl" => :build
  uses_from_macos "zlib"

  conflicts_with "rakudo-star"

  resource "Pod::Parser" do
    on_system :linux, macos: :sonoma_or_newer do
      url "https://cpan.metacpan.org/authors/id/M/MA/MAREKR/Pod-Parser-1.67.tar.gz"
      sha256 "5deccbf55d750ce65588cd211c1a03fa1ef3aaa15d1ac2b8d85383a42c1427ea"
    end
  end

  def install
    if OS.linux? || MacOS.version >= :sonoma
      ENV.prepend_create_path "PERL5LIB", buildpath/"build_deps/lib/perl5"
      resource("Pod::Parser").stage do
        system "perl", "Makefile.PL", "INSTALL_BASE=#{buildpath}/build_deps"
        system "make", "install"
      end
    end

    system "perl", "Configure.pl", "--prefix=#{prefix}",
                                   "--mandir=#{man}",
                                   "--debugging=0",
                                   "--cc=#{ENV.cc}"

    system "make"
    system "make", "install"
    # Don't install this file in HOMEBREW_PREFIX/lib
    rm_r(lib/"VERSION")
  end

  test do
    path = testpath/"test.pir"
    path.write <<~PARROT
      .sub _main
        .local int i
        i = 0
      loop:
        print i
        inc i
        if i < 10 goto loop
      .end
    PARROT

    assert_equal "0123456789", shell_output("#{bin}/parrot #{path}")
  end
end
