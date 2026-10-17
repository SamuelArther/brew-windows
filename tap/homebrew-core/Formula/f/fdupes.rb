class Fdupes < Formula
  desc "Identify or delete duplicate files"
  homepage "https://github.com/adrianlopezroche/fdupes"
  url "https://github.com/adrianlopezroche/fdupes/releases/download/v2.4.0/fdupes-2.4.0.tar.gz"
  sha256 "527b27a39d031dcbe1d29a220b3423228c28366c2412887eb72c25473d7b1736"
  license "MIT"
  version_scheme 1

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "fba6eca6083a6e091191c81f59434b36de2c2618adfe9bdbfc48d3cf5c3d45b6"
  end

  depends_on "pcre2"

  uses_from_macos "ncurses"
  uses_from_macos "sqlite"

  def install
    system "./configure", *std_configure_args.reject { |s| s["--disable-debug"] }
    system "make"
    system "make", "install"
  end

  test do
    touch "a"
    touch "b"

    dupes = shell_output("#{bin}/fdupes .").strip.split("\n").sort
    assert_equal ["./a", "./b"], dupes
  end
end
