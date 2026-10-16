class Gdl < Formula
  desc "GNOME Docking Library provides docking features for GTK+ 3"
  homepage "https://gitlab.gnome.org/Archive/gdl"
  url "https://download.gnome.org/sources/gdl/3.40/gdl-3.40.0.tar.xz"
  sha256 "3641d4fd669d1e1818aeff3cf9ffb7887fc5c367850b78c28c775eba4ab6a555"
  license "LGPL-2.0-or-later"
  revision 2

  deprecate! date: "2025-01-15", because: :repo_archived
  disable! date: "2026-01-15", because: :repo_archived

  depends_on "gettext" => :build
  depends_on "gobject-introspection" => :build
  depends_on "intltool" => :build
  depends_on "pkgconf" => [:build, :test]

  depends_on "cairo"
  depends_on "gdk-pixbuf"
  depends_on "glib"
  depends_on "gtk+3"
  depends_on "libxml2"

  uses_from_macos "perl" => :build

  on_macos do
    depends_on "at-spi2-core"
    depends_on "gettext"
    depends_on "harfbuzz"
    depends_on "pango"
  end

  on_linux do
    depends_on "perl-xml-parser" => :build
  end

  # Fix build with libxml2 2.12. Remove if upstream PR is merged and in release.
  patch do
    url "https://gitlab.gnome.org/GNOME/gdl/-/commit/414f83eb4ad9e5576ee3d089594bf1301ff24091.diff"
    sha256 "715c804e6d03304bc077b99f667bbeb062c873b3bbd737182fb2cd47a295de95"
    type :unofficial
    resolves "https://gitlab.gnome.org/GNOME/gdl/-/merge_requests/4"
  end

  def install
    system "./configure", "--disable-silent-rules",
                          "--enable-introspection=yes",
                          *std_configure_args
    system "make", "install"
  end

  test do
    (testpath/"test.c").write <<~C
      #include <gdl/gdl.h>

      int main(int argc, char *argv[]) {
        GType type = gdl_dock_object_get_type();
        return 0;
      }
    C

    pkgconf_flags = shell_output("pkgconf --cflags --libs gdl-3.0").chomp.split
    system ENV.cc, "test.c", *pkgconf_flags, "-o", "test"
    system "./test"
  end
end
