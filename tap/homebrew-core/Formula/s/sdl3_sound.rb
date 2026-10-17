class Sdl3Sound < Formula
  desc "Abstract soundfile decoder"
  homepage "https://icculus.org/SDL_sound/"
  url "https://github.com/icculus/SDL_sound/releases/download/v3.2.0/SDL3_sound-3.2.0.tar.gz"
  sha256 "3348f3ecc653e21f2b7ed8e409f07c67b179fbbfba3b99c85698e6e5d7b5924c"
  license all_of: [
    "Zlib",
    { any_of: ["LGPL-2.1-or-later", "Artistic-1.0-Perl"] },
  ]
  head "https://github.com/icculus/SDL_sound.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "006964e3e994df288f75f32b762a06760dba3768abb67dcb804dd26b74a1783b"
  end

  depends_on "cmake" => :build
  depends_on "pkgconf" => :test

  depends_on "sdl3"

  uses_from_macos "perl" => :build

  deny_network_access!

  def install
    args = %w[
      -DSDLSOUND_BUILD_DOCS=OFF
      -DSDLSOUND_BUILD_STATIC=OFF
      -DSDLSOUND_BUILD_TEST=OFF
      -DSDLSOUND_DECODER_MIDI=ON
      -DSDLSOUND_INSTALL_MAN=ON
    ]
    system "cmake", "-S", ".", "-B", "build", *args, *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.c").write <<~C
      #include <SDL3_sound/SDL_sound.h>
      #include <stdlib.h>

      int main() {
        return Sound_Version() == SDL_SOUND_VERSION ? EXIT_SUCCESS : EXIT_FAILURE;
      }
    C
    flags = shell_output("pkgconf --cflags --libs sdl3-sound").chomp.split
    system ENV.cc, "test.c", "-o", "test", *flags
    system "./test"
  end
end
