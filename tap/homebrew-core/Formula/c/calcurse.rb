class Calcurse < Formula
  desc "Text-based personal organizer"
  homepage "https://calcurse.org/"
  url "https://calcurse.org/files/calcurse-4.9.0.tar.gz"
  sha256 "8d4c36c868cc37d50decb49423548320488e64693d01527a272d1c2fb0afa8e6"
  license "BSD-2-Clause"

  livecheck do
    url "https://calcurse.org/downloads/"
    regex(/href=.*?calcurse[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "cb7a009ff0e9b5a80add80090e760a361a901d3d1fc8c7d62b9670e1409a43a1"
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

  deny_network_access!

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
