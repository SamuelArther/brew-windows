class Libgdata < Formula
  desc "GLib-based library for accessing online service APIs"
  homepage "https://wiki.gnome.org/Projects/libgdata"
  url "https://download.gnome.org/sources/libgdata/0.18/libgdata-0.18.1.tar.xz"
  sha256 "dd8592eeb6512ad0a8cf5c8be8c72e76f74bfe6b23e4dd93f0756ee0716804c7"
  license "LGPL-2.1-or-later"
  revision 2

  # Last release on 2021-03-05. Currently has no maintainers[^1][^2], required
  # old `libsoup@2` and was dropped from official GNOME back in 44.0 release[^3].
  # [^1]: https://gitlab.gnome.org/GNOME/libgdata/-/merge_requests/49#note_2224724
  # [^2]: https://gitlab.gnome.org/GNOME/libgdata/-/merge_requests/47#note_2030129
  # [^3]: https://gitlab.gnome.org/GNOME/gnome-build-meta/-/merge_requests/1854
  deprecate! date: "2025-09-05", because: :unmaintained
  disable! date: "2026-09-05", because: :unmaintained

  depends_on "gettext" => :build
  depends_on "gobject-introspection" => :build
  depends_on "meson" => :build
  depends_on "ninja" => :build
  depends_on "pkgconf" => [:build, :test]
  depends_on "vala" => :build

  depends_on "glib"
  depends_on "gtk+3"
  depends_on "json-glib"
  depends_on "liboauth"
  depends_on "libsoup@2" # libsoup 3 PR: https://gitlab.gnome.org/GNOME/libgdata/-/merge_requests/49

  uses_from_macos "curl"
  uses_from_macos "libxml2"

  on_macos do
    depends_on "gettext"
  end

  def install
    ENV.prepend_path "PKG_CONFIG_PATH", formula_opt_lib("libsoup@2")/"pkgconfig"
    ENV.prepend_path "XDG_DATA_DIRS", Formula["libsoup@2"].opt_share
    ENV.prepend_path "XDG_DATA_DIRS", HOMEBREW_PREFIX/"share"

    curl_lib = OS.mac? ? "#{MacOS.sdk_path}/usr/lib" : formula_opt_lib("curl")
    ENV.append "LDFLAGS", "-L#{curl_lib} -lcurl"

    args = %w[
      -Dintrospection=true
      -Doauth1=enabled
      -Dalways_build_tests=false
      -Dvapi=true
      -Dgtk=enabled
      -Dgoa=disabled
      -Dgnome=disabled
    ]

    system "meson", "setup", "build", *args, *std_meson_args
    system "meson", "compile", "-C", "build", "--verbose"
    system "meson", "install", "-C", "build"
  end

  test do
    (testpath/"test.c").write <<~C
      #include <gdata/gdata.h>

      int main(int argc, char *argv[]) {
        GType type = gdata_comment_get_type();
        return 0;
      }
    C

    ENV.prepend_path "PKG_CONFIG_PATH", formula_opt_lib("icu4c")/"pkgconfig" if OS.mac?
    ENV.prepend_path "PKG_CONFIG_PATH", formula_opt_lib("libsoup@2")/"pkgconfig"
    flags = shell_output("pkgconf --cflags --libs libgdata").chomp.split
    system ENV.cc, "test.c", "-o", "test", *flags
    system "./test"
  end
end
