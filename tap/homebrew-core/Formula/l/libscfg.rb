class Libscfg < Formula
  desc "C library for scfg"
  homepage "https://codeberg.org/emersion/libscfg"
  url "https://codeberg.org/emersion/libscfg/archive/v0.2.0.tar.gz"
  sha256 "cf37ef00ac8efb28821dac1ad49e2c6b23b242d9d961fab6fcda72fc73a7291b"
  license "MIT"
  head "https://codeberg.org/emersion/libscfg.git", branch: "master"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/libscfg-0.2.0"
    sha256 cellar: :any, x86_64_windows: "2f35c67a7c27cf7c909ed78e4a8111512a8759e64488c0698eaa6950be9bc4ea"
  end

  depends_on "meson" => :build
  depends_on "ninja" => :build

  def install
    system "meson", "setup", "build", *std_meson_args
    system "meson", "compile", "-C", "build", "--verbose"
    system "meson", "install", "-C", "build"
  end

  test do
    (testpath/"test.cfg").write <<~EOS
      key1 = value1
      key2 = value2
    EOS

    (testpath/"test.c").write <<~C
      #include <stdio.h>
      #include <stdlib.h>
      #include "scfg.h"

      int main() {
        const char* testFilePath = "test.cfg";
        struct scfg_block block = {0};

        int loadResult = scfg_load_file(&block, testFilePath);
        printf("Successfully loaded '%s'.\\n", testFilePath);

        for (size_t i = 0; i < block.directives_len; i++) {
          printf("Directive: %s\\n", block.directives[i].name);
          for (size_t j = 0; j < block.directives[i].params_len; j++) {
            printf("  Parameter: %s\\n", block.directives[i].params[j]);
          }
        }

        scfg_block_finish(&block);

        return 0;
      }
    C

    system ENV.cc, "test.c", "-I#{include}", "-L#{lib}", "-lscfg", "-o", "test"
    assert_match "Successfully loaded 'test.cfg'", shell_output("./test")
  end
end
