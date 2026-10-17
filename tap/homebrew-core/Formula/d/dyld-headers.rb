class DyldHeaders < Formula
  desc "Header files for the dynamic linker"
  homepage "https://opensource.apple.com/"
  url "https://github.com/apple-oss-distributions/dyld/archive/refs/tags/dyld-1378.tar.gz"
  sha256 "509c8b081153a7b9ff08d6ddf0a681c0ab5190e1e336ef2e9748b801bdb9c59e"
  license "APSL-2.0"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/dyld-headers-1378"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "3256c169b2839271b9e6f7a90c4262c63659a9c17b3be81dc5775af8ceee1268"
  end

  keg_only :provided_by_macos

  def install
    include.install Dir["include/*"]
  end

  test do
    (testpath/"test.c").write <<~C
      #include <stdio.h>
      #include <mach-o/fixup-chains.h>

      int main(void) {
        struct dyld_chained_fixups_header header = { .fixups_version = 0, .imports_format = DYLD_CHAINED_IMPORT };
        printf("%zu %u %d\\n", sizeof(header), header.imports_format, DYLD_CHAINED_PTR_ARM64E);
        return 0;
      }
    C

    system ENV.cc, "test.c", "-I#{include}", "-o", "test"
    assert_equal "28 1 1", shell_output("./test").strip
  end
end
