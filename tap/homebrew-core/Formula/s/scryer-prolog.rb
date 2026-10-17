class ScryerProlog < Formula
  desc "Modern ISO Prolog implementation written mostly in Rust"
  homepage "https://www.scryer.pl"
  license "BSD-3-Clause"
  head "https://github.com/mthom/scryer-prolog.git", branch: "master"

  stable do
    url "https://github.com/mthom/scryer-prolog/archive/refs/tags/v0.10.0.tar.gz"
    sha256 "353eca4eea539e0a0eedb1572736bfcc64c5fb6fd2da3b8737c513ad62f60f3b"

    # Backport only the openssl and openssl-sys version updates from
    # https://github.com/mthom/scryer-prolog/commit/70d046499a7ecab6006e41d7340202efa1109aa0
    patch :DATA
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "da9d44dbd3b9f9a4d55a95332292f84b45ed2af2dcc1ae05a73dfa358da0e669"
  end

  depends_on "pkgconf" => :build
  depends_on "rust" => :build

  on_linux do
    depends_on "openssl@4"
  end

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    ENV["OPENSSL_DIR"] = formula_opt_prefix("openssl@4") if OS.linux?

    system "cargo", "install", *std_cargo_args
  end

  test do
    (testpath/"test.pl").write <<~PL
      test :-
        write('Hello from Scryer Prolog').
    PL

    assert_equal "Hello from Scryer Prolog", shell_output("#{bin}/scryer-prolog -g 'test,halt' #{testpath}/test.pl")
  end
end

__END__
diff --git a/Cargo.lock b/Cargo.lock
index 4fa61925..903db1d3 100644
--- a/Cargo.lock
+++ b/Cargo.lock
@@ -1949,15 +1949,14 @@ checksum = "d6790f58c7ff633d8771f42965289203411a5e5c68388703c06e14f24770b41e"
 
 [[package]]
 name = "openssl"
-version = "0.10.72"
+version = "0.10.80"
 source = "registry+https://github.com/rust-lang/crates.io-index"
-checksum = "fedfea7d58a1f73118430a55da6a286e7b044961736ce96a16a17068ea25e5da"
+checksum = "a45fa2aa886c42762255da344f0a0d313e254066c46aad76f300c3d3da62d967"
 dependencies = [
  "bitflags 2.9.0",
  "cfg-if",
  "foreign-types",
  "libc",
- "once_cell",
  "openssl-macros",
  "openssl-sys",
 ]
@@ -1981,9 +1980,9 @@ checksum = "d05e27ee213611ffe7d6348b942e8f942b37114c00cc03cec254295a4a17852e"
 
 [[package]]
 name = "openssl-sys"
-version = "0.9.108"
+version = "0.9.116"
 source = "registry+https://github.com/rust-lang/crates.io-index"
-checksum = "e145e1651e858e820e4860f7b9c5e169bc1d8ce1c86043be79fa7b7634821847"
+checksum = "f28a22dc7140cda5f096e5e7724a6962ca81a7f8bfd2979f9b18c11af56318c4"
 dependencies = [
  "cc",
  "libc",
