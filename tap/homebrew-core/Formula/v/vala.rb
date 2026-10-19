class Vala < Formula
  desc "Compiler for the GObject type system"
  homepage "https://wiki.gnome.org/Projects/Vala"
  url "https://download.gnome.org/sources/vala/0.56/vala-0.56.19.tar.xz"
  sha256 "5ad7cbbfcc0de61b403d6797c9ef60455bfbebd8e162aec33b5b0b097adfb9d5"
  license "LGPL-2.1-or-later"
  revision 1

  depends_on "gobject-introspection" => :build
  depends_on "glib"
  depends_on "graphviz"
  depends_on "pkgconf"

  uses_from_macos "bison" => :build
  uses_from_macos "flex" => :build
  uses_from_macos "libxslt" => :build # for xsltproc

  on_macos do
    depends_on "gettext"
  end

  def install
    system "./configure", "--disable-silent-rules", *std_configure_args
    system "make" # Fails to compile as a single step
    system "make", "install"
  end

  test do
    ENV.prepend_path "PKG_CONFIG_PATH", formula_opt_lib("libffi")/"pkgconfig"
    test_string = "Hello Homebrew\n"
    path = testpath/"hello.vala"
    path.write <<~VALA
      void main () {
        print ("#{test_string}");
      }
    VALA

    valac_args = [
      # Build with debugging symbols.
      "-g",
      # Use Homebrew's default C compiler.
      "--cc=#{ENV.cc}",
      # Save generated C source code.
      "--save-temps",
      # Vala source code path.
      path.to_s,
    ]

    system bin/"valac", *valac_args
    assert_path_exists testpath/"hello.c"

    assert_equal test_string, shell_output("#{testpath}/hello")
  end
end
