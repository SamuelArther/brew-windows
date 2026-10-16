class Pcb < Formula
  desc "Interactive printed circuit board editor"
  homepage "http://pcb.geda-project.org/"
  url "https://downloads.sourceforge.net/project/pcb/pcb/pcb-4.3.0/pcb-4.3.0.tar.gz"
  sha256 "ae852f46af84aba7f51d813fb916fc7fcdbeea43f7134f150507024e1743fb5e"
  license "GPL-2.0-or-later"
  revision 1
  version_scheme 1

  head do
    url "git://git.geda-project.org/pcb.git"
    depends_on "autoconf" => :build
    depends_on "automake" => :build
  end

  # Original homepage http://pcb.geda-project.org/ redirects to
  # http://www.repo.hu/projects/geda-archive/pcb.geda-project.org/ which states
  # > WARNING: the gEDA/pcb project is not actively developed anymore.
  # > You may want to switch to the Ringdove EDA project which is similar in spirit but is active.
  deprecate! date: "2025-09-06", because: :unmaintained # TODO: replacement_formula: "pcb-rnd"
  disable! date: "2026-09-06", because: :unmaintained # TODO: replacement_formula: "pcb-rnd"

  depends_on "intltool" => :build
  depends_on "pkgconf" => :build

  depends_on "cairo"
  depends_on "dbus"
  depends_on "gd"
  depends_on "gdk-pixbuf"
  depends_on "gettext"
  depends_on "glib"
  depends_on "gtk+"
  depends_on "gtkglext"

  uses_from_macos "bison" => :build
  uses_from_macos "flex" => :build
  uses_from_macos "perl" => :build
  uses_from_macos "tcl-tk"

  on_macos do
    depends_on "at-spi2-core"
    depends_on "gnu-sed"
    depends_on "harfbuzz"
    depends_on "libxrender"
    depends_on "pango"
  end

  on_linux do
    depends_on "perl-xml-parser" => :build
  end

  conflicts_with "gts", because: "both install a `gts.h` header"

  def install
    if OS.mac?
      ENV.prepend_path "PATH", formula_opt_libexec("gnu-sed")/"gnubin"
    else
      ENV.prepend_path "PERL5LIB", formula_opt_libexec("perl-xml-parser")/"lib/perl5"
    end

    system "./autogen.sh" if build.head?
    args = %w[
      --disable-update-desktop-database
      --disable-update-mime-database
      --disable-gl
    ]
    args << "--without-x" if OS.mac?

    system "./configure", *args, *std_configure_args
    system "make", "install"
  end

  test do
    # Disable test on Linux because it fails with:
    # Gtk-WARNING **: 09:09:35.919: cannot open display
    return if OS.linux? && ENV["HOMEBREW_GITHUB_ACTIONS"]

    assert_match version.to_s, shell_output("#{bin}/pcb --version")
  end
end
