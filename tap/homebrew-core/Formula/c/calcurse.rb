class Calcurse < Formula
  desc "Text-based personal organizer"
  homepage "https://calcurse.org/"
  url "https://calcurse.org/files/calcurse-4.8.2.tar.gz"
  sha256 "849ba852c7f37b6772365cb0c42a94cde0fe75efba91363e96a0e7ef797ba565"
  license "BSD-2-Clause"

  livecheck do
    url "https://calcurse.org/downloads/"
    regex(/href=.*?calcurse[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "ddee309d88265edbdd0d462c201c3df3b64f69660923b50461a2ba6133fbc854"
  end

  head do
    url "https://git.calcurse.org/calcurse.git", branch: "master"

    depends_on "asciidoc" => :build
    depends_on "autoconf" => :build
    depends_on "automake" => :build
  end

  depends_on "pkgconf" => :build
  uses_from_macos "ncurses"

  on_macos do
    depends_on "gettext"
  end

  def install
    system "./autogen.sh" if build.head?

    system "./configure", *std_configure_args.reject { |s| s["--disable-debug"] }

    # Specify XML_CATALOG_FILES for asciidoc
    system "make", "XML_CATALOG_FILES=/usr/local/etc/xml/catalog"
    system "make", "install"
  end

  test do
    system bin/"calcurse", "-v"
  end
end
