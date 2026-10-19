class Sdl3Ttf < Formula
  desc "Library for using TrueType fonts in SDL applications"
  homepage "https://github.com/libsdl-org/SDL_ttf"
  url "https://github.com/libsdl-org/SDL_ttf/releases/download/release-3.2.2/SDL3_ttf-3.2.2.tar.gz"
  sha256 "63547d58d0185c833213885b635a2c0548201cc8f301e6587c0be1a67e1e045d"
  license "Zlib"
  head "https://github.com/libsdl-org/SDL_ttf.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "2fbd7b7f47c66de9ef0780289fdfcecf594150c1d7fb38d9785659bef4538470"
  end

  depends_on "cmake" => :build
  depends_on "pkgconf" => :build
  depends_on "freetype"
  depends_on "harfbuzz"
  depends_on "sdl3"

  uses_from_macos "perl" => :build

  deny_network_access!

  def install
    system "cmake", "-S", ".",
                    "-B", "build",
                    "-DSDLTTF_PLUTOSVG=OFF",
                    "-DSDLTTF_INSTALL_MAN=ON",
                    "-DSDLTTF_STRICT=ON",
                    "-DSDLTFF_VENDORED=OFF",
                    *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.c").write <<~C
      #include <SDL3_ttf/SDL_ttf.h>
      #include <stdlib.h>

      int main() {
        return TTF_Version() == SDL_TTF_VERSION ? EXIT_SUCCESS : EXIT_FAILURE;
      }
    C
    system ENV.cc, "test.c", "-I#{formula_opt_include("sdl3")}", "-L#{lib}", "-lSDL3_ttf", "-o", "test"
    system "./test"
  end
end
