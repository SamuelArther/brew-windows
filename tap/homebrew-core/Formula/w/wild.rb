class Wild < Formula
  desc "Very fast linker for Linux"
  homepage "https://github.com/wild-linker/wild"
  url "https://github.com/wild-linker/wild/archive/refs/tags/0.10.0.tar.gz"
  sha256 "99ec83404558d4d0cbde9dd44b8c6fa2a511a2f8bb04a31f54c0929ec4491990"
  license any_of: ["Apache-2.0", "MIT"]

  bottle do
    sha256 cellar: :any, x86_64_windows: "504360bcfdf1a779345c9718a1362bb7cbd375c3531218b33afd7adfe93cc1e3"
  end

  depends_on "rust" => :build

  on_macos do
    depends_on "binutils" => :test
  end

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", "--profile=dist", *std_cargo_args(path: "wild")
    bin.install_symlink "wild" => "ld.wild"
  end

  test do
    (testpath/"test.c").write <<~C
      int main(void) { return 0; }
    C

    linker_flag = case ENV.compiler
    when /^gcc(-(\d|1[0-5]))?$/
      # https://github.com/wild-linker/wild#cc-autotools-cmake-meson-etc
      (testpath/"wild").install_symlink bin/"wild" => "ld"
      "-B#{testpath}/wild"
    when :clang, /^gcc-\d{2,}$/ then "-fuse-ld=wild"
    else odie "unexpected compiler"
    end

    extra_flags = %w[-fPIE -pie]
    extra_flags += %w[--target=x86_64-unknown-linux-gnu -nostdlib] unless OS.linux?

    system ENV.cc, linker_flag, *extra_flags, "test.c", "-o", "test"
    if OS.linux?
      system "./test"
    else
      assert_match "ELF 64-bit LSB pie executable, x86-64", shell_output("file test")
    end

    readelf = OS.mac? ? formula_opt_bin("binutils")/"readelf" : DevelopmentTools.locate("readelf")
    assert_match "Linker: Wild ", shell_output("#{readelf} --string-dump .comment test")
  end
end
