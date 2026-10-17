class Wordplay < Formula
  desc "Anagram generator"
  homepage "https://hsvmovies.com/static_subpages/personal_orig/wordplay/"
  url "https://hsvmovies.com/static_subpages/personal_orig/wordplay/wordplay722.tar.Z"
  version "7.22"
  sha256 "9436a8c801144ab32e38b1e168130ef43e7494f4b4939fcd510c7c5bf7f4eb6d"
  # From readme:
  # This program was written for fun and is free.  Distribute it as you please,
  # but please distribute the entire package, with the original words721.txt and
  # the readme file.  If you modify the code, please mention my name in it as the
  # original author.  Please send me a copy of improvements you make, because I
  # may include them in a future version.
  license :cannot_represent

  livecheck do
    url :homepage
    regex(/href=.*?wordplay[._-]?v?(\d+(?:\.\d+)*)\.t/i)
    strategy :page_match do |page, regex|
      # Naively convert a version string like `722` to `7.22`
      page.scan(regex).map { |match| match.first.sub(/^(\d)(\d+)$/, '\1.\2') }
    end
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "0b32d196affb8c9f57b5e78366008926ea5b3bebdde7d9fb3b99b7a0d53d8861"
  end

  no_autobump! because: :incompatible_version_format

  # Fixes compiler warnings on Darwin, via MacPorts.
  # Point to words file in share.
  patch :p0 do
    file "Patches/wordplay/patch-wordplay.c"
    type :unofficial
  end

  def install
    inreplace "wordplay.c", "@PREFIX@", prefix
    system "make", "CC=#{ENV.cc}"
    bin.install "wordplay"
    pkgshare.install "words721.txt"
  end

  test do
    assert_equal "BREW", shell_output("#{bin}/wordplay -s ERWB").strip
  end
end
