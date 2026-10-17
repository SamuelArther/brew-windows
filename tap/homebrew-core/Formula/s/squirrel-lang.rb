class SquirrelLang < Formula
  desc "High level, imperative, object-oriented programming language"
  homepage "http://www.squirrel-lang.org"
  url "https://downloads.sourceforge.net/project/squirrel/squirrel3/squirrel%203.2%20stable/squirrel_3_2_stable.tar.gz"
  sha256 "211f1452f00b24b94f60ba44b50abe327fd2735600a7bacabc5b774b327c81db"
  license "MIT"
  head "https://github.com/albertodemichelis/squirrel.git", branch: "master"

  livecheck do
    url :stable
    regex(%r{url=.*?/squirrel[._-]v?(\d+(?:[_-]\d+)+)[._-]stable\.t}i)
    strategy :sourceforge do |page, regex|
      page.scan(regex).map { |match| match.first.tr("_", ".") }
    end
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/squirrel-lang-3.2"
    sha256 cellar: :any, x86_64_windows: "c71c9cde9d9d9705273b9e8f646d5ea375a65f2203d9ecf344beeb4f345ef3b0"
  end

  no_autobump! because: :incompatible_version_format

  conflicts_with "sequoia-sq", "sq", because: "both install `sq` binaries"

  def install
    # The tarball files are in a subdirectory, unlike the upstream repository.
    # Moving tarball files out of the subdirectory allows us to use the same
    # build steps for stable and HEAD builds.
    squirrel_subdir = "squirrel#{version.major}"
    if Dir.exist?(squirrel_subdir)
      mv Dir["squirrel#{version.major}/*"], "."
      rmdir squirrel_subdir
    end

    system "make"
    prefix.install %w[bin include lib]
    doc.install Dir["doc/*.pdf"]
    doc.install %w[etc samples]
    # See: https://github.com/Homebrew/homebrew/pull/9977
    (lib/"pkgconfig/libsquirrel.pc").write pc_file
  end

  def pc_file
    <<~PC
      prefix=#{opt_prefix}
      exec_prefix=${prefix}
      libdir=/${exec_prefix}/lib
      includedir=/${prefix}/include
      bindir=/${prefix}/bin
      ldflags=  -L/${prefix}/lib

      Name: libsquirrel
      Description: squirrel library
      Version: #{version}

      Requires:
      Libs: -L${libdir} -lsquirrel -lsqstdlib
      Cflags: -I${includedir}
    PC
  end

  test do
    (testpath/"hello.nut").write <<~NUT
      print("hello");
    NUT
    assert_equal "hello", shell_output("#{bin}/sq #{testpath}/hello.nut").chomp
  end
end
