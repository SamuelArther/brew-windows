class Apophenia < Formula
  desc "C library for statistical and scientific computing"
  homepage "https://github.com/b-k/apophenia"
  url "https://github.com/b-k/apophenia/archive/refs/tags/v1.0.tar.gz"
  sha256 "c753047a9230f9d9e105541f671c4961dc7998f4402972424e591404f33b82ca"
  license "GPL-2.0-only"
  revision 1

  # GPL-2.0-only code cannot use a library under GPL-3.0-or-later, see
  # https://www.gnu.org/licenses/gpl-faq.html#AllCompatibility
  #
  # In the past there was an exception to allow distributing binaries but it was removed:
  # https://github.com/b-k/apophenia/commit/3e3fe1bfea36348f82e5efa9d49fe39e217263f4
  # https://github.com/b-k/apophenia/commit/ce77bfccb94e68ae6ab28d07e44b4324c886c0e8
  disable! date: "2026-05-16", because: "needs `gsl` but is under an incompatible license"

  depends_on "gsl"

  uses_from_macos "sqlite"

  on_macos do
    depends_on "autoconf" => :build
    depends_on "automake" => :build
    depends_on "libtool" => :build
  end

  # Fix compilation with POSIX basename(3)
  # Patches already accepted upstream, remove on next release
  patch do
    file "Patches/apophenia/posix-basename.diff"
    type :backport
  end

  def install
    # Regenerate configure to avoid binaries being built with flat namespace
    system "autoreconf", "--force", "--install", "--verbose" if OS.mac?

    args = std_configure_args - ["--disable-debug"]
    system "./configure", *args, "--disable-silent-rules"
    system "make", "install"
  end

  test do
    (testpath/"foo.csv").write <<~CSV
      thud,bump
      1,2
      3,4
      5,6
      7,8
    CSV

    expected_gnuplot_output = <<~EOS
      plot '-' with lines
          1\t    2
          3\t    4
          5\t    6
          7\t    8
    EOS

    system bin/"apop_text_to_db", testpath/"foo.csv", "bar", testpath/"baz.db"
    sqlite_output = shell_output("sqlite3 baz.db '.mode csv' '.headers on' 'select * from bar'")
    assert_equal (testpath/"foo.csv").read, sqlite_output.gsub(/\r\n?/, "\n")

    query_output = shell_output("#{bin}/apop_plot_query -d #{testpath/"baz.db"} -q 'select thud,bump from bar' -f-")
    assert_equal query_output, expected_gnuplot_output
  end
end
